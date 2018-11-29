using System;
using System.Collections;
using System.Diagnostics;
using Assets;
using UnityEngine;
using Random = System.Random;

public class Spawner : MonoBehaviour {
   private readonly Stopwatch timer = new Stopwatch();

   [SerializeField] private Tracer tracerPrefab;
   [SerializeField] private Missile missilePrefab;
   [SerializeField] private Material trailMaterialBase;

   public void Start() {
      timer.Start();
      StartCoroutine(MainLoop());
   }

   private Random globalRandom = new Random(9);
   private IEnumerator MainLoop() {
      for (var i = 0;; i++) {
         var tracer = Instantiate(tracerPrefab, transform);
         tracer.transform.position = transform.position;
         var dir = Vector3.Lerp(globalRandom.NextVector3CosineWeightedHemisphere().XZY(), Vector3.up, 0.6f);
         if (i % 4 >= 2) {
            dir.z = -Math.Abs(dir.z) - 0.8f;
         }

         tracer.LinearVelocity = dir.normalized * 8 + Vector3.up * 3;
         yield return new WaitForSeconds(0.3f);

         var random = new Random(globalRandom.Next());
         var missile = Instantiate(missilePrefab, transform);
         missile.transform.position = transform.position;
         missile.transform.localScale *= 4;
         missile.RigidBody.LinearVelocity = Vector3.right * 2.0f;
         missile.DestinationTransformOptional = tracer.transform;
         missile.transform.LookAt(transform.position + missile.RigidBody.LinearVelocity, random.NextVector3UnitCircleXY().ZXY());

         InitMissile(missile);

         switch (i / 4) {
            case 0:
            case 1:
               StartCoroutine(MissileSplit(new[] { -2.0f }, missile, 5.0f));
               break;
            case 2:
               StartCoroutine(MissileSplit(new[] { -2.0f, 0.8f }, missile, 5.0f));
               break;
         }

         missile.MissileTrailContext = new GameObject { name = "MTC" };
         AddTrailHostToMissile(random, missile, 0);

//         yield break;
         yield return new WaitForSeconds(8f);
      }
   }

   private void InitMissile(Missile missile) {
      missile.Config.DeadReckoningActivationRange += missile.Config.DeadReckoningActivationRangeSpread * globalRandom.NextFloat();
   }

   private void AddTrailHostToMissile(Random random, Missile missile, int depth) {
      var trailHost = new GameObject();
      trailHost.transform.parent = missile.transform;
      trailHost.transform.localPosition = Vector3.zero;
      missile.TrailHost = trailHost;

      var tr = trailHost.AddComponent<TrailRenderer>();
      tr.material = new Material(trailMaterialBase) {
         color = random.NextHueColor()
      };
      tr.startWidth = tr.endWidth = 0.03f / ((float)Math.Pow(2.5, depth));
   }

   private IEnumerator MissileSplit(float[] timesToSplit, Missile missile, float leafTimeToDeath, int depth = 0) {
      var timeToSplit = timesToSplit[depth];
      if (timeToSplit < 0) {
         yield return new WaitForSeconds(-timeToSplit);
         while (missile.transform.forward.y > 0.7) {
            yield return new WaitForSeconds(0.1f);
         }
      } else {
         yield return new WaitForSeconds(timeToSplit);
      }

      for (var i = 0; i < 4; i++) {
         var random = new Random(globalRandom.Next());
         var x = missile.transform.right;
         var y = missile.transform.forward;
         var z = missile.transform.up;
         var r = Vector3.Lerp(random.NextVector3CosineWeightedHemisphere().XZY(), Vector3.up, 0.5f);
         var velocityDirection = r.x * x + r.y * y + r.z * z;

         var clone = Instantiate(missilePrefab, transform);
         clone.transform.position = missile.transform.position;
         clone.transform.localScale = missile.transform.localScale / 2;
         clone.RigidBody.LinearVelocity = missile.RigidBody.LinearVelocity.magnitude * velocityDirection;
//         clone.transform.LookAt(clone.transform.position + clone.RigidBody.LinearVelocity, Vector3.Cross(clone.RigidBody.LinearVelocity.normalized, random.NextVector3CosineWeightedHemisphere()));
         clone.transform.LookAt(clone.transform.position + clone.RigidBody.LinearVelocity, Vector3.Cross(clone.RigidBody.LinearVelocity.normalized, random.NextVector3CosineWeightedHemisphere()));
         clone.DestinationTransformOptional = missile.DestinationTransformOptional;
//         clone.AlerpCollapseMillis = missile.AlerpCollapseMillis * 0.8f;
//         clone.VlerpCollapseMillis = missile.VlerpCollapseMillis * 0.5f;
//         clone.ThrusterActivationDelay = (float)random.NextDouble() * 0.2f + 0.1f;
         clone.Config.NormalTerminalVelocity = missile.Config.NormalTerminalVelocity * 1.5f;
         clone.Config.DeadReckoningTerminalVelocity = missile.Config.DeadReckoningTerminalVelocity * 1.5f;
         clone.MissileTrailContext = missile.MissileTrailContext;
         InitMissile(missile);
         AddTrailHostToMissile(random, clone, depth + 1);

         if (depth + 1 < timesToSplit.Length) {
            StartCoroutine(MissileSplit(timesToSplit, clone, leafTimeToDeath, depth + 1));
         } else {
            StartCoroutine(EnableDeadReckoningAfterSeconds(clone, 2.5f));
            StartCoroutine(DestroyMissileAfterSeconds(clone, leafTimeToDeath));
         }
      }

      missile.TrailHost.GetComponent<TrailRenderer>().emitting = false;
      missile.TrailHost.transform.parent = missile.MissileTrailContext.transform;
      Destroy(missile.gameObject);
   }

   private IEnumerator EnableDeadReckoningAfterSeconds(Missile missile, float seconds) {
      yield return new WaitForSeconds(seconds);
      missile.LocomotionController.StartDeadReckoningIfNotStarted();
   }

   private IEnumerator DestroyMissileAfterSeconds(Missile missile, float seconds) {
      yield return new WaitForSeconds(seconds);
      if (missile.TrailHost && missile.MissileTrailContext) {
         missile.TrailHost.transform.parent = missile.MissileTrailContext.transform;
         StartCoroutine(DestroyAfterSeconds(missile.MissileTrailContext, 10));
      }

      Destroy(missile.gameObject);
   }

   private IEnumerator DestroyAfterSeconds(GameObject obj, float seconds) {
      yield return new WaitForSeconds(seconds);
      Destroy(obj);
   }
}
