﻿using UnityEngine;

// TODO Add a handbrake: Applying handbrake should lessen carGrip and increase turnspeed as long as the handbrake is being held.
//      Or maybe you can even momentarily change the ForceMode to VelocityChange while the brake is being held. 
// TODO Disable throttle and turning when car is in the air (wheels still can turn, and throttle can still rev)
//      Ground box collision can be a trigger and make a boolean that can be checked in FixedUpdate(). If carOnGround = true
//      then run all the code in FixedUpdate(). Otherwise return so nothing happens.

public class CarMovement : MonoBehaviour {
    public float SteerThrow = 45f;
    public float EnginePower = 1650f;
    public float TireGrip = 140f;
    public float RedlineRPM = 8000f;
    public float SteerSpeed = 45f; // deg/sec
    public float Drag = 10f;
    public float MaxSpeed = 25f;

    public Transform CoM;
    public Transform[] Wheels;
    public Transform[] Steering;
    public Transform[] TireContact;

    public int[] PowerContact;

    private Vector3[] GroundNormal;
    private Vector3[] TireForces;

    public float EnginePitchMin = 1f;
    public float EnginePitchMax = 2f;

    private Rigidbody rbody;

    private AudioSource audioSrc;

    private float ThrottleInput;
    private float SteerInput;
    private float SteerRotation;
    private float EngineRPM;
    private bool HandBrake;
    private float[] WheelSpin;

    void Start() {
        rbody = GetComponent<Rigidbody>();
        rbody.centerOfMass = CoM.localPosition;
        audioSrc = GetComponent<AudioSource>();

        GroundNormal = new Vector3[TireContact.Length];
        WheelSpin = new float[Wheels.Length];
        TireForces = new Vector3[TireContact.Length];
    }

    private void FixedUpdate() {
        // Increment Engine RPM
        if (ThrottleInput != 0f)
            EngineRPM += Mathf.Abs(ThrottleInput) * RedlineRPM * Time.fixedDeltaTime;
        else
            EngineRPM -= RedlineRPM * Time.fixedDeltaTime;
        EngineRPM = Mathf.Clamp(EngineRPM, 0, RedlineRPM);

        // Rotate front tires for steering
        if (SteerInput != 0f)
            SteerRotation += SteerInput * SteerSpeed * Time.fixedDeltaTime;
        else
            SteerRotation += Mathf.Min(Mathf.Abs(SteerRotation), SteerSpeed * Time.fixedDeltaTime) * -Mathf.Sign(SteerRotation);
         SteerRotation = Mathf.Clamp(SteerRotation, -SteerThrow, SteerThrow);
         Quaternion steer = Quaternion.Euler(0, SteerRotation, 0);

        for (int i = 0; i < Steering.Length; i++)
            Steering[i].localRotation = steer;

        // Split engine power over all wheels that provide power
        float engineForce = ThrottleInput * EnginePower / PowerContact.Length;
        for (int i = 0; i < PowerContact.Length; i++) {
            if (GroundNormal[PowerContact[i]].sqrMagnitude < .001f) continue;
            Vector3 dir = Vector3.ProjectOnPlane(TireContact[PowerContact[i]].forward, GroundNormal[PowerContact[i]]);
            rbody.AddForceAtPosition(dir * engineForce, TireContact[PowerContact[i]].position); // apply power from the contact patch from each wheel that receives engine power
        }

        // Grip force
        for (int i = 0; i < TireContact.Length; i++) {
            if (GroundNormal[i].sqrMagnitude < .001f) continue;
            // compute velocity of car relative to the tire
            Vector3 groundVel = Vector3.ProjectOnPlane(rbody.GetPointVelocity(TireContact[i].position), GroundNormal[i]);
            Vector3 relVel = TireContact[i].InverseTransformVector(groundVel);
            WheelSpin[i] += relVel.z / Time.fixedDeltaTime / .304f; // TODO: make .304 (the wheel radius) not hard-coded

            relVel.y = relVel.z = 0;

            float a = .7f;
            float b = 2f;
            float k = .309f;

            float x = a * (Mathf.Abs(relVel.x) - k);
            float force = (a * (x - k) * Mathf.Exp(1 - a * (x - k)) + b) / (b + 1);

            TireForces[i] = TireContact[i].right * -Mathf.Sign(relVel.x) * force * TireGrip;

            rbody.AddForceAtPosition(TireForces[i], TireContact[i].position);
            Wheels[i].localRotation = Quaternion.Euler(0, 0, WheelSpin[i] * Mathf.Deg2Rad);

            //TODO: This is a cheating handbrake. It increases TireForces and doesn't slow the car down
            if (HandBrake)
            {
                rbody.AddForceAtPosition(TireForces[i] * 3.0f, TireContact[i].position);
                // Simulates rear wheels locking up when handbrake is engaged 
                Wheels[2].localRotation = Quaternion.Euler(0, 0, 0);
            }
        }

        // Drag
        rbody.AddForce(-rbody.velocity * Vector3.Dot(rbody.velocity, rbody.velocity) * Drag);
    }

    void Update() {
        // Handle input
        SteerInput = Input.GetAxis("Horizontal");
        ThrottleInput = Input.GetAxis("Vertical");
        HandBrake = Input.GetKey("space");
    }

    private void LateUpdate() {
        audioSrc.pitch = Mathf.Lerp(EnginePitchMin, EnginePitchMax, EngineRPM / RedlineRPM);

        Debug.Log(Mathf.Round(rbody.velocity.magnitude) + " MPH");

    }

    private void OnCollisionEnter(Collision collision) {
        for (int i = 0; i < collision.contacts.Length; i++)
            for (int j = 0; j < Wheels.Length; j++)
                if (collision.contacts[i].thisCollider.transform == Wheels[j])
                    GroundNormal[j] = collision.contacts[i].normal;
    }
    private void OnCollisionStay(Collision collision) {
        for (int i = 0; i < collision.contacts.Length; i++)
            for (int j = 0; j < Wheels.Length; j++)
                if (collision.contacts[i].thisCollider.transform == Wheels[j])
                    GroundNormal[j] = collision.contacts[i].normal;
    }
    private void OnCollisionExit(Collision collision) {
        for (int i = 0; i < collision.contacts.Length; i++)
            for (int j = 0; j < Wheels.Length; j++)
                if (collision.contacts[i].thisCollider.transform == Wheels[j])
                    GroundNormal[j] = Vector3.zero;
    }
}