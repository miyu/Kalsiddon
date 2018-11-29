using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tracer : CustomMissileRigidBody {
   private float spawnTime;

   public void Awake() {
      spawnTime = Time.time;
   }

   public void Update() {
      var t = Time.time - spawnTime;
//      transform.position = 2.0f * new Vector3(Mathf.Cos(Time.time), 0, Mathf.Sin(Time.time)) +
//                           0.5f * new Vector3(0, Mathf.Sin(Time.time * Mathf.Exp(1)), Mathf.Cos(Time.time * Mathf.Exp(1))) +
//                           new Vector3(5, 1.1f, 0);
      transform.position = new Vector3(4 + t, 1.1f, 0.05f * Mathf.Sin(Time.time));
   }
}
