using UnityEngine;

// Borrowed a lot of ideas from the previous car script I was using.
// Flipping the steering when driving in reverse and faking inertia with SlowVelocity()
// Borrowed RotateVisualWheels() so front wheels can turn visually
// TODO wheels can spin visually but spin on their own and aren't tied to the speed of the car

public class CarMovement : MonoBehaviour
{
    public float throttle;
    public float horizontal;
    public Transform carTransform;

    public Transform LFWheelTransform;
    public Transform RFWheelTransform;
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;
    private Transform[] wheelTransforms = new Transform[4]; // transforms for the 2 front wheels
    //public Vector3 carVelo;

    private float actualTurn;
    private Rigidbody carRigidbody;
    private Vector3 carForward;
    private Vector3 relativeVelocity;
    private Vector3 com = new Vector3(0f, 0.1f, .15f);

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        carRigidbody = GetComponent<Rigidbody>();
        carForward = Vector3.forward;
        carRigidbody.centerOfMass = com; //center of mass defined neg value used to keep car from flipping
        SetUpWheels();
    }

    void Update()
    {
        CheckInput();
        CarPhysicsUpdate();
        SlowVelocity();
    }

    private void LateUpdate()
    {
        RotateVisualWheels();
    }


    void CheckInput()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer
             || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
        {
            // Use the keyboard for car input
            horizontal = Input.GetAxis("Horizontal");
            throttle = Input.GetAxis("Vertical");
        }
    }

    void CarPhysicsUpdate()
    {
        //carVelo = carRigidbody.velocity;

        carTransform = transform;

        // appling forward force using car's position vector NOTE: not sure how to use Time.deltatime here
        carRigidbody.AddForce(carTransform.forward * (throttle * 30f), ForceMode.Force);

        actualTurn = horizontal;

        // flipping steering when driving in reverse
        if (throttle < 0.0f)
        {
            actualTurn = -actualTurn;
        }

        // NOTE: Time.deltatime required here
        // Turn the car using torque
        carRigidbody.AddTorque(0f, actualTurn * 30f, 0f, ForceMode.Force);

        // Need to research what's happening here, borrowed from old script
        relativeVelocity = carTransform.InverseTransformDirection(carTransform.forward);
    }

    void SetUpWheels()
    {
        if (frontLeftWheel == null || frontRightWheel == null)
        {
            Debug.LogError("One of more of the wheel transforms have not been plugged into the car");
            Debug.Break();
        }
        else
        {
            wheelTransforms[0] = frontLeftWheel;
            wheelTransforms[1] = frontRightWheel;
            wheelTransforms[2] = rearLeftWheel;
            wheelTransforms[3] = rearRightWheel;
        }
    }

    private Vector3 rotationAmount;

    void RotateVisualWheels()
    {
        LFWheelTransform.localEulerAngles = new Vector3(0f, horizontal * 30f, 0f);
        RFWheelTransform.localEulerAngles = new Vector3(0f, horizontal * 30f, 0f);

        rotationAmount = carForward * (relativeVelocity.z * 1.6f * Time.deltaTime * Mathf.Rad2Deg);

        wheelTransforms[0].Rotate(rotationAmount);
        wheelTransforms[1].Rotate(rotationAmount);
        wheelTransforms[2].Rotate(rotationAmount);
        wheelTransforms[3].Rotate(rotationAmount);
    }

    void SlowVelocity()
    {
        // letting inertia naturally slow the car down
        carRigidbody.AddForce(-carTransform.forward * 1.9f);
    }
}

