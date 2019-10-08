using UnityEngine;
using System.Text;

// TODO Add a handbrake: Applying handbrake should lessen carGrip and increase turnspeed as long as the handbrake is being held.
//      Or maybe you can even momentarily change the ForceMode to VelocityChange while the brake is being held. 
// TODO Disable throttle and turning when car is in the air (wheels still can turn, and throttle can still rev)
//      Ground box collision can be a trigger and make a boolean that can be checked in FixedUpdate(). If carOnGround = true
//      then run all the code in FixedUpdate(). Otherwise return so nothing happens.

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(CarMovement))]
public class CarMovementEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        CarMovement car = target as CarMovement;

        EditorGUI.BeginChangeCheck();
        AnimationCurve pitch = EditorGUILayout.CurveField("Engine Pitch", car.EnginePitch, Color.green, new Rect(0f, 0f, 1f, 10f));
        AnimationCurve lateralGrip = EditorGUILayout.CurveField("Laterial Grip", car.LateralGrip, Color.green, new Rect(0f, 0f, 1f, 1f));
        AnimationCurve spinSlip = EditorGUILayout.CurveField("Spin Grip", car.SpinGrip, Color.green, new Rect(0f, 0f, 1f, 1f));
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(car, "Edit AnimationCurve");
            car.EnginePitch = pitch;
            car.LateralGrip = lateralGrip;
            car.SpinGrip = spinSlip;
        }
    }
}
#endif

public class CarMovement : MonoBehaviour {
    public float SteerThrow = 45f;
    public float EngineTorque = 10f;
    public float LateralGripMultiplier = 140f;
    public float SlipForceMultiplier = 140f;
    public float SteerSpeed = 45f; // deg/sec
    public float MaxSpeed = 50f; // m/s
    public float MaxSlip = 1f;

    public UnityEngine.UI.Text SpeedText;

    public Transform CoM;
    public Transform[] Wheels;
    public Transform[] Steering;
    public Transform[] TireContact;
    public int[] PowerContact;

    // gonna show these with a custom editor (see above)
    [HideInInspector]
    public AnimationCurve EnginePitch;
    [HideInInspector]
    public AnimationCurve LateralGrip;
    [HideInInspector]
    public AnimationCurve SpinGrip;

    private float carSpeed;
    private Vector3[] groundNormal; // the normal of the ground under each tire

    private ParticleSystem[] tireSmoke;

    private Rigidbody rbody;

    private AudioSource engineAudio;

    private float tireRadius;
    private float throttleInput;
    private float steerInput;
    private float steerRotation;
    private bool handBrake;
    private float[] wheelSpeed;
    private float[] tireRotation;

    void Start() {
        rbody = GetComponent<Rigidbody>();
        rbody.centerOfMass = CoM.localPosition;
        engineAudio = GetComponent<AudioSource>();

        groundNormal = new Vector3[TireContact.Length];
        wheelSpeed = new float[Wheels.Length];
        tireRotation = new float[Wheels.Length];

        tireRadius = Wheels[0].GetComponent<SphereCollider>().radius;

        tireSmoke = new ParticleSystem[TireContact.Length];
        for (int i = 0; i < TireContact.Length; i++)
            tireSmoke[i] = TireContact[i].GetComponent<ParticleSystem>();
    }

    private void FixedUpdate() {
        // Rotate front tires for steering
        float desiredSteer = steerInput * SteerThrow;
        float steerStep = SteerSpeed * Time.fixedDeltaTime;
        if (Mathf.Abs(desiredSteer - steerRotation) < steerStep)
            steerRotation = desiredSteer; // if the amount remaining is smaller than the step size, then just snap to the desired angle (rather than overshooting).
        else
            steerRotation += steerStep * Mathf.Sign(desiredSteer - steerRotation);
        Quaternion steer = Quaternion.Euler(0, steerRotation, 0);
        for (int i = 0; i < Steering.Length; i++)
            Steering[i].localRotation = steer;

        // increase the wheelSpeed of the tires that are under power
        float desiredWheelSpeed = throttleInput * MaxSpeed * tireRadius;
        for (int i = 0; i < PowerContact.Length; i++) {
            float step = (Mathf.Abs(throttleInput) + 1f) * EngineTorque * Time.fixedDeltaTime;
            if (Mathf.Abs(desiredWheelSpeed - wheelSpeed[PowerContact[i]]) < step)
                wheelSpeed[PowerContact[i]] = desiredWheelSpeed;
            else
                wheelSpeed[PowerContact[i]] += step * Mathf.Sign(desiredWheelSpeed - wheelSpeed[PowerContact[i]]);

            // hard limit for max speed: do not allow tires to spin faster than MaxSpeed
            wheelSpeed[PowerContact[i]] = Mathf.Min(wheelSpeed[PowerContact[i]], MaxSpeed * tireRadius);
        }

        // Tire physics
        for (int i = 0; i < TireContact.Length; i++) {
            if (groundNormal[i].sqrMagnitude < .01f) continue; // Don't do anything if the tire is not on the ground

            // Compute velocity of car relative to the tire
            Vector3 groundVel = Vector3.ProjectOnPlane(rbody.GetPointVelocity(TireContact[i].position), groundNormal[i]);
            Vector3 relVel = TireContact[i].InverseTransformVector(groundVel);

            // lateral grip: comes just from the lateral (right/left) velocity of the wheel
            // Lateral grip goes exactly right or left of the wheel, thus keeping the car "on track"
            float lateralForce = LateralGrip.Evaluate(Mathf.Abs(relVel.x) / MaxSpeed);

            // slip comes from the difference in the linear speed of the bottom of the tire (from spinning), and the actual linear ground speed
            float slip = (wheelSpeed[i] * tireRadius - relVel.z);

            // spinForce scales with the ratio of wheelSpeed * radius / (fwdVel + 1)
            float spinForce = Mathf.Sign(slip) * SpinGrip.Evaluate(Mathf.Abs(slip) / MaxSlip);

            rbody.AddForceAtPosition(TireContact[i].right * -Mathf.Sign(relVel.x) * lateralForce * LateralGripMultiplier, TireContact[i].position);
            rbody.AddForceAtPosition(TireContact[i].forward * spinForce * SlipForceMultiplier, TireContact[i].position);

            wheelSpeed[i] += -spinForce * tireRadius * Time.fixedDeltaTime;
            tireRotation[i] += wheelSpeed[i] * Time.fixedDeltaTime;

            Wheels[i].localRotation = Quaternion.Euler(0, 0, tireRotation[i] * Mathf.Rad2Deg);

            ParticleSystem.EmissionModule em = tireSmoke[i].emission;
            em.enabled = Mathf.Abs(slip) > MaxSlip * .5f || Mathf.Abs(relVel.x) > MaxSpeed * .5f;
        }

        carSpeed = rbody.velocity.magnitude;

        SpeedText.text = (carSpeed * 2.23694f).ToString("0") + " mph"; // m/s -> mph
    }

    void Update() {
        // Handle input
        steerInput = Input.GetAxis("Horizontal");
        throttleInput = Input.GetAxis("Vertical");
        handBrake = Input.GetKey("space");
        engineAudio.pitch = EnginePitch.Evaluate(carSpeed / MaxSpeed);

        for (int i = 0; i < Wheels.Length; i++)
            Debug.DrawLine(Wheels[i].position, Wheels[i].position + groundNormal[i]);
    }

    private void OnCollisionEnter(Collision collision) {
        for (int i = 0; i < collision.contacts.Length; i++)
            for (int j = 0; j < Wheels.Length; j++)
                if (collision.contacts[i].thisCollider.transform == Wheels[j])
                    groundNormal[j] = collision.contacts[i].normal;
    }
    private void OnCollisionStay(Collision collision) {
        for (int i = 0; i < collision.contacts.Length; i++)
            for (int j = 0; j < Wheels.Length; j++)
                if (collision.contacts[i].thisCollider.transform == Wheels[j])
                    groundNormal[j] = collision.contacts[i].normal;
    }
    private void OnCollisionExit(Collision collision) {
        for (int i = 0; i < collision.contacts.Length; i++)
            for (int j = 0; j < Wheels.Length; j++)
                if (collision.contacts[i].thisCollider.transform == Wheels[j])
                    groundNormal[j] = Vector3.zero;
    }
}