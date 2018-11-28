using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

public class Missile : CustomPhysics {
   public Tracer Tracer;

   [SerializeField] private Material DeadReckoningMaterial;
   private readonly Stopwatch timer = new Stopwatch();
   private ThrustBehaviour[] thrusters;

   private float AlerpCollapseMillis = 1000.0f;
   public float VlerpCollapseMillis = 80000.0f;
   public bool DeadReckoningEnabled;
   public Vector3 DeadReckoningDirectionUnitWorld;
   internal float DeadReckoningDistanceThreshold = 1;

   public float ControlSystemBootDelay = 0;
   public float ThrusterActivationDelay = 0;

   internal float NormalTerminalVelocity = 5;
   internal float DeadReckoningTerminalVelocity = 8;

   private float Mass = 1;
   private Vector3 MomentOfInertia = Vector3.one * 0.001f;
   

   public GameObject CenterOfMass;
   public GameObject MissileTrailContext;
   public GameObject TrailHost;

   private VernierPropulsionContributions vernierPropulsionContributions;

   public void Awake() {
      thrusters = GetComponentsInChildren<ThrustBehaviour>();
//      thrusters = new [] { thrusters[0], thrusters[1] };
   }

   public void Start() {
      timer.Start();
   }

   public override void FixedUpdate () {
      Step(Time.fixedDeltaTime);

      // Sorta jank: Perform physics logic after our updates.
      base.FixedUpdate();
   }

   private Vector3 ComputeSeekDirectionUnit() {
      var vToTarget = Tracer.transform.position - transform.position;
      if (vToTarget.magnitude < DeadReckoningDistanceThreshold) {
         StartDeadReckoningIfNotStarted();
      }

      var seekDirectionUnitWorld = DeadReckoningEnabled
         ? DeadReckoningDirectionUnitWorld
         : vToTarget.normalized;
      //      seekDirectionUnitWorld = new Vector3(0.1f, 1.0f, 0).normalized;
      return seekDirectionUnitWorld;
   }

   private void Step(float dt) {
      ControlSystemBootDelay -= dt;
      if (ControlSystemBootDelay > 0) return; // No locally applied forces.

      ThrusterActivationDelay -= dt;
      if (ThrusterActivationDelay > 0) return; // In future, will use to enable/disable perfect aim.
      
      var seekDirectionUnitWorld = ComputeSeekDirectionUnit();

      // Compute Vernier propulsion contribution
      if (Time.frameCount % 8 == 0) {
         vernierPropulsionContributions = ComputeVernierThrusterContributions(seekDirectionUnitWorld);
      }
      var localToWorld = transform.localToWorldMatrix;
      var linearAccelerationWorld = localToWorld.MultiplyVector(vernierPropulsionContributions.LinearAccelerationLocal);
      var angularAccelerationWorld = localToWorld.MultiplyVector(vernierPropulsionContributions.AngularAccelerationLocal);
      linearAccelerationWorld *= 15;
      angularAccelerationWorld *= 10;

      // Compute main propulsion contribution
      var mainPropulsionAlignment = Mathf.Max(0, Vector3.Dot(transform.forward, seekDirectionUnitWorld));
      linearAccelerationWorld += transform.forward * Mathf.Lerp(0.6f, 1.0f, mainPropulsionAlignment) * 3;

      AngularVelocity += angularAccelerationWorld * dt;
      AngularVelocity *= 0.99f;

      var alerp = Mathf.Lerp(0.5f, 0.8f, timer.ElapsedMilliseconds / AlerpCollapseMillis);
      var actualAcceleration = Vector3.Lerp(linearAccelerationWorld.normalized, seekDirectionUnitWorld, alerp) * linearAccelerationWorld.magnitude;
      LinearVelocity += actualAcceleration * dt;

      var vlerp = 0.1f; // Mathf.Lerp(0.0f, 1.0f, timer.ElapsedMilliseconds / VlerpCollapseMillis);
      LinearVelocity = Vector3.Lerp(LinearVelocity.normalized, actualAcceleration.normalized, vlerp) * LinearVelocity.magnitude;

      var velocityCap = DeadReckoningEnabled ? DeadReckoningTerminalVelocity : NormalTerminalVelocity;
      LinearVelocity = Math.Min(LinearVelocity.magnitude, velocityCap) * Vector3.Lerp(LinearVelocity.normalized, seekDirectionUnitWorld.normalized, vlerp * vlerp);
      Debug.Log(LinearVelocity.magnitude);
   }

   /// <summary>
   /// Similar semantics to dot product of unit vectors.
   /// 1: identical rotations
   /// 0: orthogonal rotations
   /// -1: opposite rotations
   /// </summary>
   private float ScoreQ(Quaternion qInitial, Quaternion qPredicted, Vector3 forwardDesired) {
      var forwardInitial = qInitial * Vector3.forward;
      var forwardPredicted = qPredicted * Vector3.forward;
      var initialToDesired = (forwardDesired - forwardInitial).normalized;
      var initialToPredicted = (forwardPredicted - forwardInitial).normalized;
      return Vector3.Dot(initialToDesired, initialToPredicted);
   }

   private VernierPropulsionContributions ComputeVernierThrusterContributions(Vector3 seekDirectionUnitWorld) {
      var linearAccelerationWorld = Vector3.zero;
      var torqueWorld = Vector3.zero;

      var centerOfMassWorld = CenterOfMass.transform.position;
      var thrusterStrengthSum = thrusters.Sum(t => t.Strength);
      var invMomentLocal = new Vector3(1.0f / MomentOfInertia.x, 1.0f / MomentOfInertia.y, 1.0f / MomentOfInertia.z);

      var forceSideThrusters = 1; // normalize for now, scale at executive controller
      var qInitial = transform.rotation.normalized;

      foreach (var thruster in thrusters) {
         var sensorDirectionUnit = -thruster.Sensor.transform.forward;
         var thrustDirectionUnit = -thruster.transform.forward;
//         sensorDirectionUnit = thrustDirectionUnit;

         var r = thruster.transform.position - centerOfMassWorld;

         // Maximum thrust/torque
         var thrustMagnitude = forceSideThrusters * (thruster.Strength / thrusterStrengthSum);
         var maximumThrustWorld = forceSideThrusters * (thruster.Strength / thrusterStrengthSum) * thrustDirectionUnit;
         var maximumTorqueWorld = Vector3.Cross(r, maximumThrustWorld);

         // And from sensor's biased perspective (which we'll use for control system)
         var sensorMaximumThrustWorld = thrustMagnitude * sensorDirectionUnit;
         var sensorMaximumTorqueWorld = Vector3.Cross(r, sensorMaximumThrustWorld);

         // Thruster power percentage proportional to alignment of sensor & seek direction.
         var linearAlignment = Vector3.Dot(sensorDirectionUnit, seekDirectionUnitWorld);

         var dqDt = Helper.DqDtOmegaWorld(sensorMaximumTorqueWorld * Time.fixedDeltaTime * 20, qInitial);
         var qPredicted = Helper.Add(qInitial, dqDt);
         var qs = ScoreQ(qInitial, qPredicted, seekDirectionUnitWorld);
         var rotationalAlignment = qs; // Mathf.Max(0, qs);

         var power = linearAlignment * 0.3f + rotationalAlignment + 0.3f;
         power = Mathf.Clamp01(power);

//         Debug.DrawLine(thruster.transform.position,
//            thruster.transform.position + qPredicted * Vector3.forward * 5,
//            Color.magenta);
//
//         Debug.DrawLine(thruster.transform.position,
//            thruster.transform.position + qPredicted * Vector3.forward * 5 * power,
//            Color.cyan,
//            0,
//            false);

         // Aggregate force, compute torque
         var aggregateForceWorld = maximumThrustWorld * power;
         var aggregateTorqueWorld = maximumTorqueWorld * power;

         // Update accelerations
         linearAccelerationWorld += aggregateForceWorld / Mass;
         torqueWorld += aggregateTorqueWorld;
      }

      var worldToLocal = transform.worldToLocalMatrix;
      return new VernierPropulsionContributions(
         worldToLocal.MultiplyVector(linearAccelerationWorld),
         Vector3.Scale(worldToLocal.MultiplyVector(torqueWorld), invMomentLocal));
   }

   public struct VernierPropulsionContributions {
      public Vector3 LinearAccelerationLocal;
      public Vector3 AngularAccelerationLocal;

      public VernierPropulsionContributions(Vector3 linearAccelerationLocal, Vector3 angularAccelerationLocal) {
         LinearAccelerationLocal = linearAccelerationLocal;
         AngularAccelerationLocal = angularAccelerationLocal;
      }
   }

   public void OnCollisionEnter(Collision collision) {
      Debug.Log("!!");
   }

   public void StartDeadReckoningIfNotStarted() {
      if (DeadReckoningEnabled) return;
      var vToTarget = Tracer.transform.position - transform.position;

      gameObject.name = "DR";
      DeadReckoningEnabled = true;
      DeadReckoningDirectionUnitWorld = vToTarget.normalized;

      foreach (var renderer in GetComponentsInChildren<Renderer>()) {
         if (renderer is TrailRenderer) continue;
         renderer.sharedMaterial = DeadReckoningMaterial;
      }
   }
}

public class CustomPhysics : MonoBehaviour {
   protected static readonly Vector3 g = Vector3.down * 9.8f;
   public Vector3 LinearVelocity;
   public Vector3 AngularVelocity;

   public virtual void FixedUpdate() {
//      LinearVelocity = Vector3.zero;
//      AngularVelocity = new Vector3(0, 0, Mathf.PI);

      transform.position += LinearVelocity * Time.fixedDeltaTime;
      if (GetComponent<Missile>()) {
//         LinearVelocity += g * Time.fixedDeltaTime * 0.1f;
      } else {
//         LinearVelocity += g * Time.fixedDeltaTime;
         transform.position = 2.0f * new Vector3(Mathf.Cos(Time.time), 0, Mathf.Sin(Time.time)) +
                              0.5f * new Vector3(0, Mathf.Sin(Time.time * Mathf.Exp(1)), Mathf.Cos(Time.time * Mathf.Exp(1))) +
                              new Vector3(5, 1.1f, 0);
         return;
      }


      if (!GetComponent<Missile>()) {
         return;
      }

      var qInitial = transform.rotation.normalized;
      var dqDt = Helper.DqDtOmegaWorld(AngularVelocity * Time.fixedDeltaTime, qInitial);
      var qFinal = Helper.Add(qInitial, dqDt);
//      AngularVelocity *= 0.99f;

      transform.rotation = qFinal;

      var omegaMax = 5;
      if (AngularVelocity.magnitude > omegaMax) {
         AngularVelocity = AngularVelocity.normalized * omegaMax;
      }
   }
}

public static class Helper {
   public static string ToStr(this Vector3 v) => $"v3({v.x} {v.y} {v.z})";
   public static string ToStr(this Quaternion v) => $"q({v.x} {v.y} {v.z} {v.w})";

   public static Quaternion DqDtOmegaWorld(Vector3 omega, Quaternion orientation) {
      var v = Quaternion.AngleAxis(omega.magnitude * Mathf.Rad2Deg * 2, omega.normalized);
      return Scale(0.5f, v * orientation);
   }

   public static Quaternion Scale(float a, Quaternion b) => new Quaternion(a * b.x, a * b.y, a * b.z, a * b.w);
   public static Quaternion Add(Quaternion a, Quaternion b) => new Quaternion(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w).normalized;
}