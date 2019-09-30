using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Axis {
    PositiveX, PositiveY, PositiveZ,
    NegativeX, NegativeY, NegativeZ
}

[RequireComponent(typeof(SphereCollider))]
public class Wheel : MonoBehaviour {

    public bool Power = false;
    public Axis SpinAxis = Axis.PositiveX;

    [HideInInspector]
    public float Torque = 0;

    void FixedUpdate() {
        
    }
}
