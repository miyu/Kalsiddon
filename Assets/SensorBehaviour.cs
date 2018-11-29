using UnityEngine;

public class SensorBehaviour : MonoBehaviour {
   public Vector3 BiasedThrustDirectionWorld => -transform.forward;
}