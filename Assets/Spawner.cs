using System;
using System.Collections;
using System.Diagnostics;
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

   private IEnumerator MainLoop() {
      var random = new System.Random();
      while (true) {
         var tracer = Instantiate(tracerPrefab, transform);
         tracer.transform.position = transform.position;
         var dir = Vector3.Lerp(random.NextVector3CosineWeightedHemisphere().XZY(), Vector3.up, 0.6f);
         tracer.Velocity = dir * 12;
         yield return new WaitForSeconds(0.3f);


         var missile = Instantiate(missilePrefab, transform);
         missile.transform.position = transform.position;
         missile.transform.localScale *= 4;
         missile.transform.LookAt(transform.position + Vector3.up, random.NextVector3UnitCircleXY().XZY());
         missile.Velocity = Vector3.up * 0.01f;
         missile.Tracer = tracer;
//         missile.StartupDelay = 0.5f;
         StartCoroutine(MissileSplit(random, new[]{-2.0f, 0.8f}, missile, 5.0f));

         missile.MissileTrailContext = new GameObject { name = "MTC" };
         AddTrailHostToMissile(random, missile);

         yield return new WaitForSeconds(8f);
      }
   }

   private void AddTrailHostToMissile(Random random, Missile missile) {
      var trailHost = new GameObject();
      trailHost.transform.parent = missile.transform;
      trailHost.transform.localPosition = Vector3.zero;
      missile.TrailHost = trailHost;

      var tr = trailHost.AddComponent<TrailRenderer>();
      tr.material = new Material(trailMaterialBase) {
         color = random.NextHueColor()
      };
      tr.startWidth = tr.endWidth = 0.1f;
   }

   private IEnumerator MissileSplit(Random random, float[] timesToSplit, Missile missile, float leafTimeToDeath, int depth = 0) {
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
         var x = missile.transform.right;
         var y = missile.transform.forward;
         var z = missile.transform.up;
         var r = Vector3.Lerp(random.NextVector3CosineWeightedHemisphere().XZY(), Vector3.up, 0.5f);
         var velocityDirection = r.x * x + r.y * y + r.z * z;

         var clone = Instantiate(missilePrefab, transform);
         clone.transform.position = missile.transform.position;
         clone.transform.localScale = missile.transform.localScale / 2;
         clone.Velocity = missile.Velocity.magnitude * velocityDirection;
         clone.transform.LookAt(clone.transform.position + clone.Velocity, Vector3.right);
         clone.Tracer = missile.Tracer;
         clone.AlerpCollapseMillis = missile.AlerpCollapseMillis * 0.8f;
         clone.VlerpCollapseMillis = missile.VlerpCollapseMillis * 0.5f;
         clone.ThrusterActivationDelay = (float)random.NextDouble() * 0.2f + 0.1f;
         clone.NormalTerminalVelocity = missile.NormalTerminalVelocity * 1.5f;
         clone.DeadReckoningTerminalVelocity = missile.DeadReckoningTerminalVelocity * 1.5f;
         clone.MissileTrailContext = missile.MissileTrailContext;
         AddTrailHostToMissile(random, clone);

         if (depth + 1 < timesToSplit.Length) {
            StartCoroutine(MissileSplit(random, timesToSplit, clone, leafTimeToDeath, depth + 1));
         } else {
            StartCoroutine(EnableDeadReckoningAfterSeconds(clone, 1.5f));
            StartCoroutine(DestroyMissileAfterSeconds(clone, leafTimeToDeath));
         }
      }

      missile.TrailHost.GetComponent<TrailRenderer>().emitting = false;
      missile.TrailHost.transform.parent = missile.MissileTrailContext.transform;
      Destroy(missile.gameObject);
   }

   private IEnumerator EnableDeadReckoningAfterSeconds(Missile missile, float seconds) {
      yield return new WaitForSeconds(seconds);
      missile.StartDeadReckoningIfNotStarted();
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

public static class RandomStatics {
   public static Vector3 NextVector3CosineWeightedHemisphere(this Random random) {
      var a = (float)Math.Abs(random.NextDouble());
      var b = (float)Math.Abs(random.NextDouble());

      var r = Mathf.Sqrt(1 - a * a);
      var phi = 2 * Mathf.PI * b;

      return new Vector3(Mathf.Cos(phi) * r, Mathf.Sin(phi) * r, a);
   }

   public static Vector3 NextVector3UnitCircleXY(this Random random) {
      var theta = (float)random.NextDouble() * (Mathf.PI * 2);
      return new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0);
   }

   public static Color NextHueColor(this Random random) {
      return Color.HSVToRGB((float)random.NextDouble(), 1, 1);
   }

   public static Vector3 XZY(this Vector3 v) => new Vector3(v.x, v.z, v.y);
}