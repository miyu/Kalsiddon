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
   private bool isDeadReckoningActivated;
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
      var seekDirection = UpdateDeadReckoningAndComputeSeekDirection();
      var netAccelerations = guidanceCalculator.ComputeNetAccelerationsWorld(seekDirection);

      // Integrate and dampen angular velocity
      rigidBody.AngularVelocity += netAccelerations.AngularAcceleration * dt;
      rigidBody.AngularVelocity *= config.AngularVelocityDampening;
      rigidBody.AngularVelocity = Vector3.ClampMagnitude(rigidBody.AngularVelocity, 5);

      // Magically correct linear acceleration, then integrate linear velocity
      var accelerationOptimality = Mathf.Lerp(0.5f, 0.9f, timer.ElapsedMilliseconds / config.AccelerationLerpCollapseMillis);
      var linearAcceleration = netAccelerations.LinearAcceleration;
      linearAcceleration = linearAcceleration.LerpDirection(seekDirection, accelerationOptimality);
      rigidBody.LinearVelocity += linearAcceleration * dt;

      // Magically correct linear velocity to match acceleration
      var velocityOptimality = 0.1f;// timer.ElapsedMilliseconds / config.VelocityLerpCollapseMillis;
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

   private Vector3 UpdateDeadReckoningAndComputeSeekDirection() {
      if (isDeadReckoningActivated) {
         return deadReckoningDirection;
      }

      var vToTarget = missile.ComputeDestination() - transform.position;
      var isInDeadReckoningRange = vToTarget.magnitude < config.DeadReckoningActivationRange;
      if (config.IsDeadReckoningFeatureEnabled && isInDeadReckoningRange) {
         StartDeadReckoning(vToTarget.normalized);
      }

      return isDeadReckoningActivated ? deadReckoningDirection : vToTarget.normalized;
   }

   public void StartDeadReckoning(Vector3 direction) {
      Assert.IsFalse(isDeadReckoningActivated);

      isDeadReckoningActivated = true;
      deadReckoningDirection = direction;

      // For debuggability, suffix dead-reckoning missiles' names
      gameObject.name += " (DR)";
   }

   public void StartDeadReckoningIfNotStarted() {
      if (isDeadReckoningActivated) return;
      Assert.IsTrue(config.IsDeadReckoningFeatureEnabled);

      var vToTarget = missile.ComputeDestination() - transform.position;
      StartDeadReckoning(vToTarget.normalized);
   }
}
