using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets {
   /// <summary>
   /// Everything should be considered `const` in update!
   /// </summary>
   public class MissileConfig : MonoBehaviour {
      public Missile Missile;

      private ThrustBehaviour[] thrusters_;
      public ThrustBehaviour[] Thrusters => thrusters_ ?? (thrusters_ = Missile.GetComponentsInChildren<ThrustBehaviour>());

      public Transform CenterOfMassTransform;
      public Vector3 CenterOfMassWorld => CenterOfMassTransform.position;

      public float Mass = 1;
      internal Vector3 MomentOfInertia = new Vector3(1f, 1f, 0.001f);
      public Vector3 InvMomentOfInertia => new Vector3(1.0f / MomentOfInertia.x, 1.0f / MomentOfInertia.y, 1.0f / MomentOfInertia.z);

      public Vector3 GravitationalAcceleration = Vector3.down * 9.8f * 0f;

      public float NormalTerminalVelocity = 4;
      public float AngularVelocityDampening = 0.99f;

      public float AccelerationLerpCollapseMillis = 1000.0f;
      public float VelocityLerpCollapseMillis = 80000.0f;

      public bool IsDeadReckoningFeatureEnabled = true;
      public float DeadReckoningActivationRange = 1;
      public float DeadReckoningActivationRangeSpread = 1.0f;
      public float DeadReckoningTerminalVelocity = 10;
   }
}
