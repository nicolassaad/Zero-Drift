using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
class Car : MonoBehaviour {
    public Transform[] Steering;

    private Wheel[] Wheels;

    public void Start() {
        Wheels = GetComponentsInChildren<Wheel>();
    }

    public void Update() {
        float steer = 0;
        if (Input.GetKey(KeyCode.A)) steer -= 1.0f;
        if (Input.GetKey(KeyCode.D)) steer += 1.0f;

        float accel = 0;
        if (Input.GetKey(KeyCode.W)) accel = 1.0f;

        for (int i = 0; i < Wheels.Length; i++)
            if (Wheels[i].Power)
                Wheels[i].Torque = accel;
    }
}
