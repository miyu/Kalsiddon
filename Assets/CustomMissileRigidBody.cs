using Assets;
using UnityEngine;

public class CustomMissileRigidBody : MonoBehaviour {
   [SerializeField] private MissileConfig config;

   public Vector3 LinearVelocity;
   public Vector3 AngularVelocity;

   public void ExecutePhysicsUpdate(float dt) {
      // Integrate Translation
      LinearVelocity += config.GravitationalAcceleration * dt;
      transform.position += LinearVelocity * Time.fixedDeltaTime;

      // Integrate Rotation
      var qInitial = transform.rotation.normalized;
      var deltaQ = MathUtils.ComputeDeltaQuaternionDqDt(AngularVelocity * Time.fixedDeltaTime, qInitial);
      var qFinal = MathUtils.Add(qInitial, deltaQ);
      transform.rotation = qFinal;
   }
}