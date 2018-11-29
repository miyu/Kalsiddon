using UnityEngine;

public class ThrustBehaviour : MonoBehaviour {
   public SensorBehaviour Sensor;
   public float Strength;

   public Vector3 ThrustDirectionWorld => -transform.forward;
}