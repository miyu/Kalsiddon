using System.Diagnostics;
using Assets;
using UnityEngine;
using UnityEngine.Assertions;
using Time = UnityEngine.Time;

public class AggregateMissileGuidanceCalculator : MonoBehaviour {
   [SerializeField] private MissileConfig config;
   [SerializeField] private VernierGuidanceCalculator vernierGuidanceCalculator;
   [SerializeField] private float vernierNetForceMaxMagnitude = 15;
   [SerializeField] private float vernierNetTorqueMagnitude = 10;
   [SerializeField] private float mainPropulsionMinSeekPower = 0.6f;
   [SerializeField] private float mainPropulsionMaxSeekPower = 1.0f;
   [SerializeField] private float mainPropulsionNetForceMaxMagnitude = 3;

   private readonly Stopwatch timer = new Stopwatch();
   private LocalDynamicsContribution vernierPropulsionNormalized;

   private void Start() {
      timer.Start();
   }

   private int t = 0;
   public WorldKinematicsContribution ComputeNetAccelerationsWorld(Vector3 seekDirection) {
      // Update normalized propulsions
      if (t++ % 64 == 0) {
         vernierPropulsionNormalized = vernierGuidanceCalculator.ComputeNormalizedLocalPropulsion(seekDirection);
      }

      var mainPropulsionNormalized = ComputeMainPropulsionLocal(seekDirection);

      // Aggregate propulsions to local-space accelerations
      var linearAccelerationLocal = 
         vernierPropulsionNormalized.Force * vernierNetForceMaxMagnitude / config.Mass +
         mainPropulsionNormalized.Force * mainPropulsionNetForceMaxMagnitude / config.Mass;
      var angularAccelerationLocal =
         Vector3.Scale(vernierPropulsionNormalized.Torque * vernierNetTorqueMagnitude, config.InvMomentOfInertia);

      // Return world-space accelerations
      var localToWorld = transform.localToWorldMatrix;
      return new WorldKinematicsContribution(
         localToWorld.MultiplyVector(linearAccelerationLocal),
         localToWorld.MultiplyVector(angularAccelerationLocal));
   }

   private LocalDynamicsContribution ComputeMainPropulsionLocal(Vector3 seekDirection) {
      var alignment = Mathf.Max(0, Vector3.Dot(transform.forward, seekDirection));
      var power = Mathf.Lerp(mainPropulsionMinSeekPower, mainPropulsionMaxSeekPower, alignment);
      var thrust = Vector3.forward * power;
      return new LocalDynamicsContribution(thrust, Vector3.zero);
   }
}

public struct MissileAccelerations {
   public Vector3 LinearAccelerationWorld;
   public Vector3 AngularAccelerationWorld;
}