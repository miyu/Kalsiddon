using System.Collections;
using System.Collections.Generic;
using Assets;
using UnityEngine;
using Random = System.Random;

public class Tracer : CustomMissileRigidBody {
   private float spawnTime;
   public int MissileCount;
   private float tOffset;

   public void Awake() {
      spawnTime = Time.time;
      tOffset = new Random().NextFloat() * 5;
   }

   public void Update() {
      var t = Time.time - spawnTime;
      var oldPosition = transform.position;
      var t2 = t + tOffset;
      transform.position = 2.0f * new Vector3(Mathf.Cos(t2), 0, Mathf.Sin(t2)) +
                           0.5f * new Vector3(0, Mathf.Sin(t2 * Mathf.Exp(1)), Mathf.Cos(t2 * Mathf.Exp(1))) +
                           new Vector3(5, 1.1f, 0);
      var t3 = t + tOffset / 5 * 2;
      transform.position = new Vector3(4 + t3 - 1, 1.1f, 0.05f * Mathf.Sin(Time.time));
      transform.LookAt(transform.position * 2 - oldPosition);
   }
}
