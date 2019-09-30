using UnityEngine;

// TODO Add a handbrake: Applying handbrake should lessen carGrip and increase turnspeed as long as the handbrake is being held.
//      Or maybe you can even momentarily change the ForceMode to VelocityChange while the brake is being held. 
// TODO Disable throttle and turning when car is in the air (wheels still can turn, and throttle can still rev)
//      Ground box collision can be a trigger and make a boolean that can be checked in FixedUpdate(). If carOnGround = true
//      then run all the code in FixedUpdate(). Otherwise return so nothing happens.
// TODO Program CheckInput to handle a Xbox controller
// TODO Wheels keep spinning forwards when car is in reverse
// TODO Add exhuast smoke and tire skid marks (using slideSpeed)
// TODO flatVelo var never worked neither in this script or the old one, causing slideSpeed, relativeVelocity, and imp to not work
// TODO how to add fake suspension

// The current values I have for the PLAYER rigidbody as well as the CarMovement vars should be used for when the handbrake is applied.
// Otherwise the car should be a little harder to turn and drift without it. 

public class CarMovement : MonoBehaviour
{
    private float throttle;
    private float horizontal;
    public Transform carTransform;

    public Transform LFWheelTransform;
    public Transform RFWheelTransform;
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    public float power = 1650f;
    public float maxSpeed = 40f;
    public float carGrip = 140f; 
    public float turnSpeed = 2f;

    public float slideSpeed;
    public float actualGrip;

    private Transform[] wheelTransforms = new Transform[4]; // transforms for the 2 front wheels
    private Vector3 velo;
    private Vector3 tempVEC;
    private Vector3 dir;
    private Vector3 flatDir;

    private float actualTurn;
    private Vector3 imp;
    private Rigidbody carRigidbody;
    private Vector3 carForward;
    private Vector3 carRight;
    private Vector3 carUp;
    private Vector3 relativeVelocity;
    private Vector3 com = new Vector3(0f, 0.1f, .15f);
    public float carSpeed;
    private Vector3 flatVelo;
    private Vector3 engineForce;
    private float rev;
    private float carMass;
    private Vector3 turnVec;
    private readonly float minSpeedToTurn = 0.8f;

    private AudioSource audioSource;
    private float audioSourcePitch;
    public AudioClip carEngine;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        carRigidbody = GetComponent<Rigidbody>();
        carMass = GetComponent<Rigidbody>().mass;
        carForward = Vector3.forward;
        carRight = Vector3.right;
        carUp = carTransform.up;
        carRigidbody.centerOfMass = com; //center of mass defined neg value used to keep car from flipping
        audioSource = GetComponent<AudioSource>();
        audioSourcePitch = GetComponent<AudioSource>().pitch;
        audioSource.clip = carEngine;
        audioSource.loop = true;
        SetUpWheels();
    }

    void Update()
    {
        // Check input for keyboard
        CheckInput();
        // Handle car physics
        CarPhysicsUpdate();
        // Slow car down with inertia * disabled for now *
        //SlowVelocity();
    }

    private void LateUpdate()
    {
        // Rotate and turn wheels
        RotateVisualWheels();
        // Engine revs with speed
        EngineSound();
    }

    private void FixedUpdate()
    {
        if (carSpeed < maxSpeed)
        {
            carRigidbody.AddForce(engineForce * Time.fixedDeltaTime, ForceMode.Force);
        }

        if (carSpeed > minSpeedToTurn)
        {
            carRigidbody.AddTorque(turnVec * Time.fixedDeltaTime, ForceMode.Force);

        } else if (minSpeedToTurn < carSpeed)
        {
            return;
        }

        // Apply forces to our rigidbody for grip
        carRigidbody.AddForce(imp * Time.fixedDeltaTime);
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
        carTransform = transform;

        // Taking out the Y values from our direction vector
        velo = carRigidbody.velocity;

        tempVEC = new Vector3(velo.x, 0f, velo.z);

        // figure out velocity without y movement - our flat velocity
        dir = transform.TransformDirection(carForward);

        tempVEC = new Vector3(dir.x, 0f, dir.z);

        // calculate our direction, removing y movement - our flat direction
        flatDir = Vector3.Normalize(tempVEC);

        flatVelo = new Vector3(velo.x, 0f, velo.z);

        flatVelo = Vector3.Normalize(flatVelo);

        // In the old script this was the first appearing of flatVelo, meaning the value will always be zero. WTF?
        //this is the key to getting the wheels to move...
        relativeVelocity = carTransform.InverseTransformDirection(flatVelo); // flatVelo has no value

        //BROKEN: Gets no value
        // calculate how much we are sliding (find out movement along x axis)
        slideSpeed = Vector3.Dot(carRight, flatVelo);

        // calculates current speed
        //BROKEN: I need flatVelo but it has no value
        carSpeed = velo.magnitude;

        // check if we're moving in reverse 
        rev = Mathf.Sign(Vector3.Dot(flatVelo, flatDir));

        // calculate engine force with our flat direction vector and acceleration 
        engineForce = (flatDir * (power * throttle) * carMass);

        // do turning
        actualTurn = horizontal;

        if (rev < 0.0f)
        {
            actualTurn = -actualTurn;
        }

        // calculating the turning vector
        turnVec = (((carUp * turnSpeed) * actualTurn) * carMass) * 800;

        // actualGrip gives accurate reading but changing carGrip value has no effect because imp doesn't work
        actualGrip = Mathf.Lerp(100, carGrip, carSpeed * 0.02f);

        // (slidespeed doesn't work so imp won't either)
        imp = carRight * (-slideSpeed * carMass * actualGrip);
    }

    // BROKEN: Wheels steer but don't rotate
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

        // relativeVelocity should be here instead of velo.magnitute
        rotationAmount = carForward * (velo.magnitude * 1.6f * Time.deltaTime * Mathf.Rad2Deg);

        wheelTransforms[0].Rotate(rotationAmount);
        wheelTransforms[1].Rotate(rotationAmount);
        wheelTransforms[2].Rotate(rotationAmount);
        wheelTransforms[3].Rotate(rotationAmount);
    }

    void SlowVelocity()
    {
        // letting inertia naturally slow the car down CURRENTLY DRAG & ANGULAR DRAG ARE DOING THE WORK 
        //carRigidbody.AddForce(-carTransform.forward * 0.8f);
    }

    void EngineSound()
    {
        audioSourcePitch = 0.30f + carSpeed * 0.025f;

        // setting a max value for the pitch
        if (audioSourcePitch > 2.0)
        {
            audioSourcePitch = 2.0f;
        }

        audioSource.pitch = audioSourcePitch;
    }
}