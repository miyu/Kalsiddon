using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Assets;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Random = System.Random;

public class MissileLocomotionController : MonoBehaviour {
   [SerializeField] private Missile missile;
   [SerializeField] private MissileConfig config;
   [SerializeField] private AggregateMissileGuidanceCalculator guidanceCalculator;
   [SerializeField] private CustomMissileRigidBody rigidBody;
   private Vector3 lastPosition;
   private bool isDeadReckoningActivated;
   private bool isPreviouslyAlignedToTarget;
   private int deadReckoningAlignmentCount;
   private Vector3 deadReckoningPosition;
   private Vector3 deadReckoningDirection;

   private readonly Stopwatch timer = new Stopwatch();
   private readonly Random random = new Random(0);

   private void Awake() {
      timer.Start();
   }

   private void FixedUpdate() {
      Step(Time.fixedDeltaTime);
   }

   public void Step(float dt) {
      var actualForward = (transform.position - lastPosition).normalized;
      lastPosition = transform.position;

      var seekDirection = UpdateDeadReckoningAndComputeSeekDirection(actualForward);
      var netAccelerations = guidanceCalculator.ComputeNetAccelerationsWorld(seekDirection);

      // Integrate and dampen angular velocity
      rigidBody.AngularVelocity += netAccelerations.AngularAcceleration * dt;
      rigidBody.AngularVelocity *= config.AngularVelocityDampening;
      rigidBody.AngularVelocity = Vector3.ClampMagnitude(rigidBody.AngularVelocity, 5);

      // Magically correct linear acceleration, then integrate linear velocity
      var accelerationOptimality = Mathf.Lerp(config.AccelerationLerpBase, 0.9f, timer.ElapsedMilliseconds / config.AccelerationLerpCollapseMillis);
      var linearAcceleration = netAccelerations.LinearAcceleration;
      linearAcceleration = linearAcceleration.LerpDirection(seekDirection, accelerationOptimality);
      rigidBody.LinearVelocity += linearAcceleration * dt;

      // Magically correct linear velocity to match acceleration
      var velocityOptimality = config.VelocityOptimality;
      rigidBody.LinearVelocity = rigidBody.LinearVelocity.LerpDirection(
         linearAcceleration.normalized,
         velocityOptimality);

      // Dampen linear velocity
      var velocityCap = isDeadReckoningActivated ? config.DeadReckoningTerminalVelocity : config.NormalTerminalVelocity;
      rigidBody.LinearVelocity = Vector3.ClampMagnitude(rigidBody.LinearVelocity, velocityCap);

      // Perform physics update
      rigidBody.ExecutePhysicsUpdate(dt);

      // Perturb local rotation to unstuck whirlyness from local maximas.
      var k = Mathf.Exp(dt) / 100; // Approx 0.01 per step when 50-tick.
      transform.localRotation = MathUtils.Add(
         transform.localRotation,
         new Quaternion(
            random.NextFloat() * k,
            random.NextFloat() * k,
            random.NextFloat() * k,
            random.NextFloat() * k
         ));
   }

   private Vector3 UpdateDeadReckoningAndComputeSeekDirection(Vector3 actualForward) {
      var vToTarget = missile.ComputeDestination() - transform.position;
      var distanceToTarget = vToTarget.magnitude;
      var vToTargetUnit = vToTarget / distanceToTarget;
      var alignmentToTarget = Vector3.Dot(vToTargetUnit, actualForward);

      if (!isDeadReckoningActivated) {
         var isInDeadReckoningRange = distanceToTarget < config.DeadReckoningActivationRange;
         if (config.IsDeadReckoningFeatureEnabled && isInDeadReckoningRange && alignmentToTarget > 0.8f) {
            StartDeadReckoning();
         } else {
            return vToTarget.normalized;
         }
      }

      if (!isPreviouslyAlignedToTarget && alignmentToTarget > 0.8f) {
         deadReckoningAlignmentCount++;
         isPreviouslyAlignedToTarget = true;
      } else if (isPreviouslyAlignedToTarget && alignmentToTarget < 0.7f) {
         isPreviouslyAlignedToTarget = false;
      }

      Debug.DrawLine(deadReckoningPosition, deadReckoningPosition + Vector3.right * 0.1f, Color.white, 0.5f);
      Debug.DrawLine(deadReckoningPosition, deadReckoningPosition + Vector3.up * 0.1f, Color.white, 0.5f);
      Debug.DrawLine(deadReckoningPosition, deadReckoningPosition + Vector3.forward * 0.1f, Color.white, 0.5f);
      var vToDeadReckoningPosition = (deadReckoningPosition - transform.position).normalized;
      var direction = Vector3.Lerp(vToDeadReckoningPosition, deadReckoningDirection, Mathf.Lerp(0.8f, 1, deadReckoningAlignmentCount / 3f));
      return Vector3.Lerp(vToTargetUnit, direction, Mathf.Lerp(0.8f, 1, deadReckoningAlignmentCount / 2f));
   }

   public void StartDeadReckoning() {
      Assert.IsFalse(isDeadReckoningActivated);

      isDeadReckoningActivated = true;
      deadReckoningPosition = missile.ComputeDestination();
      deadReckoningDirection = (deadReckoningPosition - transform.position).normalized;

      // For debuggability, suffix dead-reckoning missiles' names, color red
      gameObject.name += " (DR)";

      foreach (var renderer in GetComponentsInChildren<Renderer>()) {
         if (renderer is TrailRenderer) continue;
         renderer.sharedMaterial = missile.DeadReckoningMaterial;
      }
   }

   public void StartDeadReckoningIfNotStarted() {
      if (isDeadReckoningActivated) return;
      Assert.IsTrue(config.IsDeadReckoningFeatureEnabled);

      var vToTarget = missile.ComputeDestination() - transform.position;
      StartDeadReckoning();
   }
}
