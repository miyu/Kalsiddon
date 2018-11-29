using System;
using UnityEngine;
using Random = System.Random;

namespace Assets {
   public static class MathUtils {
      #region RNG
      public static float NextFloat(this Random random) => (float)random.NextDouble();

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
      #endregion

      #region Vector Swizzling
      public static Vector3 XZY(this Vector3 v) => new Vector3(v.x, v.z, v.y);
      public static Vector3 ZXY(this Vector3 v) => new Vector3(v.z, v.x, v.y);
      #endregion

      public static Vector3 LerpDirection(this Vector3 v, Vector3 direction, float t) {
         var vMagnitude = v.magnitude;
         return Vector3.Lerp(v / vMagnitude, direction, t) * vMagnitude;
      }

      public static string ToLongString(this Vector3 v) => $"v3({v.x} {v.y} {v.z})";
      public static string ToLongString(this Quaternion v) => $"q({v.x} {v.y} {v.z} {v.w})";

      #region Quaternion Math
      public static Quaternion ComputeDeltaQuaternionDqDt(Vector3 omega, Quaternion orientation) {
         var v = Quaternion.AngleAxis(omega.magnitude * Mathf.Rad2Deg * 2, omega.normalized);
         return Scale(0.5f, v * orientation);
      }

      public static Quaternion Scale(float a, Quaternion b) => new Quaternion(a * b.x, a * b.y, a * b.z, a * b.w);
      public static Quaternion Add(Quaternion a, Quaternion b) => new Quaternion(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w).normalized;
      #endregion
   }
}
