using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Assets {
   public class VernierGuidanceCalculator : MonoBehaviour {
      [SerializeField] private MissileConfig config;
      [SerializeField] private float seekingPower = 0.3f;
      [SerializeField] private float alignmentPower = 1;
      [SerializeField] private float constantPower = 0.3f;

      /// <summary>
      /// Vernier contributions nudge the missile's orientation toward the goal.
      /// They're biased to give the missile a spirally look.
      /// </summary>
      public LocalDynamicsContribution ComputeNormalizedLocalPropulsion(Vector3 goalDirection) {
         // Unpack rocketry parameters & capture initial pose
         var thrusters = config.Thrusters;
         var centerOfMassWorld = config.CenterOfMassWorld;
         var qInitial = transform.rotation.normalized;
         var thrusterWeightSum = thrusters.Sum(t => t.Strength);

         // Accumulate normalized thrust/torque in world-space
         var sumForceWorldNormalized = Vector3.zero;
         var sumTorqueWorldNormalized = Vector3.zero;
         foreach (var thruster in thrusters) {
            // Assume thrusters in total yield max 1 newton of thrust. This is scaled at Missile.
            var thrustWeightNormalization = thruster.Strength / thrusterWeightSum;
            
            // Use sensor-biased thrust direction to introduce systematic offset error.
            var biasedThrustDirection = thruster.Sensor.BiasedThrustDirectionWorld;

            // Compute biased thrust/torque (normalized)
            var biasedThrust = biasedThrustDirection * thrustWeightNormalization;
            var biasedR = thruster.transform.position - centerOfMassWorld; // torque = cross(r, f)
            var biasedTorque = Vector3.Cross(biasedR, biasedThrust);

            // Predict (isolated) change in orientation due to torque.
            const float tStepProjection = 0.4f; // doesn't matter too much as long as it's positive and not too big.
            var dqDt = MathUtils.ComputeDeltaQuaternionDqDt(biasedTorque * tStepProjection, qInitial);
            var qPredicted = MathUtils.Add(qInitial, dqDt);

            // alignment - align w/ goal, even if that offsets us off the straight path.
            // NOTE: Rotational alignment is permitted to be negative. This is necessary for over-spin correction
            var alignmentWeight = ComputeRotationalAlignmentScore(qInitial, qPredicted, goalDirection);

            // seeking - steer toward goal, even if that un-aligns us.
            var seekingWeight = Vector3.Dot(biasedThrustDirection, goalDirection);

            // Power determined by rotational alignment and linear alignment.
            var power = seekingWeight * seekingPower + alignmentWeight * alignmentPower + constantPower;
            power = Mathf.Clamp01(power);

            // Compute actual thruster force/torque contributions, accounting for thruster power.
            var actualThrust = thruster.ThrustDirectionWorld * thrustWeightNormalization * power;
            var actualR = thruster.transform.position - centerOfMassWorld;
            var actualTorque = Vector3.Cross(actualR, actualThrust);

            // Update accumulators
            sumForceWorldNormalized += actualThrust; 
            sumTorqueWorldNormalized += actualTorque;
         }

         var worldToLocal = transform.worldToLocalMatrix;
         return new LocalDynamicsContribution(
            worldToLocal.MultiplyVector(sumForceWorldNormalized),
            worldToLocal.MultiplyVector(sumTorqueWorldNormalized));
      }

      /// <summary>
      /// Scores orientation change from initial to predicted based whether forward vector
      /// moves toward desired goal direction world.
      /// 
      /// Conceptually like directional similarity computed via dot of unit vectors.
      /// 1: identical rotations
      /// 0: orthogonal rotations
      /// -1: opposite rotations
      /// </summary>
      private float ComputeRotationalAlignmentScore(Quaternion qInitial, Quaternion qPredicted, Vector3 goalDirection) {
         var forwardInitial = qInitial * Vector3.forward;
         var forwardPredicted = qPredicted * Vector3.forward;
         var initialToDesired = (goalDirection - forwardInitial).normalized;
         var initialToPredicted = (forwardPredicted - forwardInitial).normalized;
         return Vector3.Dot(initialToDesired, initialToPredicted);
      }
   }

   /// <summary>
   /// Represents a torque-force pair in local-space.
   /// </summary>
   public struct LocalDynamicsContribution {
      public Vector3 Force;
      public Vector3 Torque;

      public LocalDynamicsContribution(Vector3 force, Vector3 torque) {
         Force = force;
         Torque = torque;
      }
   }

   /// <summary>
   /// Represents a linear/angular acceleration pair in world-space.
   /// </summary>
   public struct WorldKinematicsContribution {
      public Vector3 LinearAcceleration;
      public Vector3 AngularAcceleration;

      public WorldKinematicsContribution(Vector3 linearAcceleration, Vector3 angularAcceleration) {
         LinearAcceleration = linearAcceleration;
         AngularAcceleration = angularAcceleration;
      }
   }
}
