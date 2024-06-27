using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class FirstPerson : MonoBehaviour
{
    [SerializeField] float speed = 5.0f; //spped for walking
    [SerializeField] float sensitivity = 2.0f; //speed for looking around
    private float rotationX = 0;

    public Transform lookAt;
    private Transform camTransform;

    private float distance = 5.0f;
    private float currentX = 0.0f;
    private float currentY = 0.0f;
    public float sensitivityX = 2.0f;
    public float sensitivityY = 2.0f;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(0, 0, speed * Time.deltaTime, Space.Self); // Move forward
        }
        else if (Input.GetKey(KeyCode.S))
        {
            Debug.Log("back");

            transform.Translate(0, 0, -speed * Time.deltaTime, Space.Self); // Move backward
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(0, -speed * Time.deltaTime, 0); // Turn left
            Debug.Log("left");

        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(0, speed * Time.deltaTime, 0);
            Debug.Log("Right");
            // Turn right
        }
        // Handle camera rotation input
        currentX += Input.GetAxis("Mouse X") * sensitivityX;
        currentY -= Input.GetAxis("Mouse Y") * sensitivityY;
    }

    private void LateUpdate()
    {
        // Clamp the vertical angle to restrict looking too high or too low
        currentY = Mathf.Clamp(currentY, -90.0f, 90.0f);

        // Calculate rotation based on current X and Y angles
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // Calculate the desired position based on rotation and distance from lookAt point
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + lookAt.position;

        // Set camera position and rotation
        camTransform.rotation = rotation;
        camTransform.position = position;
    }

    public float playerHeight;

    public Transform orientation;

    [Header("Movement")]
    public float moveSpeed;
    public float moveMultiplier;
    public float airMultiplier;
    public float counterMovement;

    public float jumpForce;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("CounterMovement")]
    public float maxSpeed;
    public float walkMaxSpeed;
    public float sprintMaxSpeed;
    public float airMaxSpeed;

    [Header("Ground Detection")]
    public LayerMask whatIsGround;
    public Transform groundCheck;
    public float groundCheckRadius;

    private float horizontalInput;
    private float verticalInput;

    public bool grounded;
    private bool canJump = true;

    private Vector3 moveDirection;
    private Vector3 slopeMoveDirection;

    private Rigidbody rb;

    RaycastHit slopeHit;

    public TextMeshProUGUI text_speed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        grounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, whatIsGround);

        MyInput();
        ControlSpeed();

        if (Input.GetKeyDown(jumpKey) && grounded)
        {
            Jump();
            canJump = false;
        }

        slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal);
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    private void ControlSpeed()
    {
        if (grounded && Input.GetKey(sprintKey))
            maxSpeed = sprintMaxSpeed;

        else if (grounded)
            maxSpeed = walkMaxSpeed;

        // no specific airMaxSpeed for now;
        //else
        //    maxSpeed = airMaxSpeed;
    }

    private void MovePlayer()
    {
        float x = horizontalInput;
        float y = verticalInput;

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        moveDirection = orientation.forward * y + orientation.right * x;

        // on slope
        if (OnSlope())
            rb.AddForce(slopeMoveDirection.normalized * moveSpeed * moveMultiplier, ForceMode.Force);

        // on ground
        else if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * moveMultiplier, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * moveMultiplier * airMultiplier, ForceMode.Force);

        // limit rb velocity
        Vector3 rbFlatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (rbFlatVelocity.magnitude > maxSpeed)
        {
            rbFlatVelocity = rbFlatVelocity.normalized * maxSpeed;
            rb.velocity = new Vector3(rbFlatVelocity.x, rb.velocity.y, rbFlatVelocity.z);
        }
    }

    private void Jump()
    {
        if (!grounded)
            return;

        // reset rb y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

        canJump = false;
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded) return;

        float threshold = 0.01f;

        //Counter movement
        if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.5f))
        {
            if (slopeHit.normal != Vector3.up)
                return true;
        }

        return false;
    }

    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject triggerObject = other.gameObject;

        GameObject Object = other.gameObject;

        if (Object.tag == "death")
        {
            Reset();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("ground"))
        {
            canJump = true;
        }
    }

    public void Reset()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
}
