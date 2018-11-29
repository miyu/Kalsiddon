using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Random = System.Random;

public class Missile : MonoBehaviour {
   public MissileConfig Config;
   public CustomMissileRigidBody RigidBody;
   public MissileLocomotionController LocomotionController;

   public Material DeadReckoningMaterial;
   public GameObject MissileTrailContext;
   public GameObject TrailHost;

   public Transform DestinationTransformOptional;
   public Func<Vector3> DestinationFuncOptional;

   public Vector3 ComputeDestination() => DestinationTransformOptional != null
      ? DestinationTransformOptional.position
      : DestinationFuncOptional();

   public void Awake() {
      var r = new Random();
      foreach (var thruster in Config.Thrusters) {
         if (r.Next() % 2 == 0) {
            var tempPosition = thruster.transform.localPosition;
            var tempRotation = thruster.transform.localRotation;
            thruster.transform.localPosition = thruster.Sensor.transform.localPosition;
            thruster.transform.localRotation = thruster.Sensor.transform.localRotation;
            thruster.Sensor.transform.localPosition = tempPosition;
            thruster.Sensor.transform.localRotation = tempRotation;
         }
         if (r.Next() % 2 == 0) {
            thruster.transform.localPosition = Vector3.Scale(thruster.transform.localPosition, new Vector3(-1, -1, 1));
            thruster.Sensor.transform.localPosition = Vector3.Scale(thruster.transform.localPosition, new Vector3(-1, -1, 1));

            var rot = Quaternion.AngleAxis(Mathf.PI, new Vector3(0, 0, 1));
            thruster.transform.localRotation *= rot;
            thruster.Sensor.transform.localRotation *= rot;
         }
         if (r.Next() % 2 == 0) {
            var rot = Quaternion.AngleAxis((float)r.NextDouble() * Mathf.PI * 2, new Vector3(0, 0, 1));
            thruster.transform.localRotation *= rot;
            thruster.Sensor.transform.localRotation *= rot;
         }
      }
   }

   public void OnCollisionEnter(Collision collision) {
      Debug.Log("!!");
   }
}