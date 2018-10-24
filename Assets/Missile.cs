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

   private readonly Stopwatch timer = new Stopwatch();
   private ThrustBehaviour[] thrusters;

   public float AlerpCollapseMillis = 4000.0f;
   public float VlerpCollapseMillis = 8000.0f;
   public bool DeadReckoningEnabled;
   public Vector3 DeadReckoningDirectionUnitWorld;
   public float DeadReckoningDistanceThreshold = 2;

   public float StartupDelay = 0;
   public float ThrusterActivationDelay = 0;

   public float NormalTerminalVelocity = 10;
   public float DeadReckoningTerminalVelocity = 20;

   public GameObject MissileTrailContext;
   public GameObject TrailHost;

   public void Awake() {
      thrusters = GetComponentsInChildren<ThrustBehaviour>();
   }

   public void Start() {
      timer.Start();
   }

   public override void FixedUpdate () {
      StartupDelay -= Time.deltaTime;
      if (StartupDelay > 0) {
         Velocity += transform.forward * g.magnitude * 2 * Time.deltaTime;
      } else {
         ThrusterActivationDelay -= Time.deltaTime;
         if (ThrusterActivationDelay <= 0) {
            RocketScience();
         }
      }

      var initialPosition = transform.position;
      base.FixedUpdate();
      var finalPosition = transform.position;
      transform.LookAt(finalPosition + (finalPosition - initialPosition), transform.up);
   }

   private void RocketScience() {
      // Acceleration: Seek tracer
      var vToTarget = Tracer.transform.position - transform.position;

      if (vToTarget.magnitude < DeadReckoningDistanceThreshold) StartDeadReckoningIfNotStarted();

      var seekDirectionUnitWorld = DeadReckoningEnabled
         ? DeadReckoningDirectionUnitWorld
         : vToTarget.normalized;
      var thrusterAcceleration = Vector3.zero;
      var totalStrength = thrusters.Sum(t => t.Strength);
      foreach (var thruster in thrusters) {
         var sensorDirectionUnit = -thruster.Sensor.transform.forward;
         var thrusterDirectionUnit = -thruster.transform.forward;
         var alignment = Mathf.Max(0, Vector3.Dot(sensorDirectionUnit, seekDirectionUnitWorld));
         thrusterAcceleration += alignment * thrusterDirectionUnit * thruster.Strength / totalStrength;
      }

      var alerp = Mathf.Lerp(0.3f, 0.8f, timer.ElapsedMilliseconds / AlerpCollapseMillis);
      var actualAcceleration = Vector3.Lerp(thrusterAcceleration, seekDirectionUnitWorld, alerp) * 10;

      var vlerp = Mathf.Lerp(0.0f, 1.0f, timer.ElapsedMilliseconds / VlerpCollapseMillis);
      Velocity += actualAcceleration * Time.deltaTime;

      var velocityCap = DeadReckoningEnabled ? DeadReckoningTerminalVelocity : NormalTerminalVelocity;
      Velocity = Math.Min(Velocity.magnitude, velocityCap) * Vector3.Lerp(Velocity.normalized, seekDirectionUnitWorld.normalized, vlerp * vlerp);
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
   }
}

public class CustomPhysics : MonoBehaviour {
   protected static readonly Vector3 g = Vector3.down * 5f;
   public Vector3 Velocity;

   public virtual void FixedUpdate() {
      transform.position += Velocity * Time.deltaTime;
      Velocity += g * Time.deltaTime;
   }
}