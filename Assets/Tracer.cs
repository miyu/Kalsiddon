using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tracer : CustomMissileRigidBody {
   private float spawnTime;
   public int MissileCount;

   public void Awake() {
      spawnTime = Time.time;
   }

   public void Update() {
      var t = Time.time - spawnTime;
      var oldPosition = transform.position;
      transform.position = 2.0f * new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)) +
                           0.5f * new Vector3(0, Mathf.Sin(t * Mathf.Exp(1)), Mathf.Cos(t * Mathf.Exp(1))) +
                           new Vector3(5, 1.1f, 0);
//      transform.position = new Vector3(4 + t, 1.1f, 0.05f * Mathf.Sin(Time.time));
      transform.LookAt(transform.position * 2 - oldPosition);
   }
}
