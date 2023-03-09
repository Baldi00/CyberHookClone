using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMover : MonoBehaviour
{
    [Header("Movement Speed")]
    [Tooltip("Speed in m/s")]
    [SerializeField]
    private float playerInitialSpeed = 5.0f;
    [Tooltip("Speed increment in m/s^2")]
    [SerializeField]
    private float speedAcceleration = 1.0f;
    [Tooltip("Speed decrement in m/s^2")]
    [SerializeField]
    private float speedDeceleration = 3.0f;
    [SerializeField]
    private float maxSpeed = 28f;
    [SerializeField]
    private float minSpeed = 5f;
    [SerializeField]
    private float inputSmoothTimer = 0.1f;

    [Header("Jump")]
    [SerializeField]
    private float jumpHeight = 1.0f;

    [Header("Camera Effects")]
    [SerializeField]
    private CinemachineVirtualCamera cinemachineVirtualCamera;
    [SerializeField]
    private float maxDutch = 1f;
    [SerializeField]
    private float initialDutchDuration = 0.5f;
    [SerializeField]
    private float finalDutchDuration = 0.25f;
    [SerializeField]
    private float maxCameraNoise = 1.5f;

    [Header("Hook")]
    [SerializeField]
    private GameObject hookPointPrefab;
    [SerializeField]
    private float hookRewindForce = 20f;
    [SerializeField]
    private float hookRewindSpeedAcceleration = 3.5f;

    private DefaultInputActions inputManager;
    private CharacterController characterController;
    //private Rigidbody rigidBody;
    private ConfigurableJoint configurableJoint;
    private Transform mainCameraTransform;

    private Vector2 rawMoveInput;
    private Vector2 smoothMoveInput;
    private Vector3 playerDesiredXZDirection;
    private float currentSpeed;
    private float currentSpeedPercentage;
    private bool isGrounded;
    private bool isJumpPressed;
    private bool isJumpPressedContinuously;
    private bool canDoubleJump;

    private Vector3 collisionWithWallNormal;
    private bool isCollidingWithWall;
    private bool isRubbingAgainstWall;

    private CinemachineBasicMultiChannelPerlin cinemachineNoise;
    private float currentDutchDuration;
    private float dutchChangeTimer;
    private bool invertDutch;

    private bool isHooking;
    private Rigidbody hookPointRigidBody;

    private float playerVelocityY;

    private float unusedCurrentVelocity1;
    private float unusedCurrentVelocity2;

    void Awake()
    {
        mainCameraTransform = Camera.main.transform;
        characterController = GetComponent<CharacterController>();
        //rigidBody = GetComponent<Rigidbody>();
        configurableJoint = GetComponent<ConfigurableJoint>();
        cinemachineNoise = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        currentSpeed = playerInitialSpeed;
        smoothMoveInput = Vector2.zero;
        canDoubleJump = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        inputManager = GameManager.Instance.inputManager;
    }

    void Update()
    {
        ReadInput();
        SmoothMoveInput();

        isGrounded = characterController.isGrounded;

        if (isGrounded)
            playerVelocityY = 0;
        else
        {
            playerVelocityY -= 9.81f * Time.deltaTime;
            characterController.Move(playerVelocityY * Time.deltaTime * Vector3.up);
        }

        //CheckCollisionWithGround();

        UpdatePlayerSpeed();
        CalculatePlayerDesiredXZDirection();
        //CheckCollisionWithWalls();

        //if (isCollidingWithWall)
        //    canDoubleJump = true;

        //isRubbingAgainstWall = isCollidingWithWall && isJumpPressedContinuously;

        //if (isRubbingAgainstWall)
        //    MovePlayerWhileRubbingOnWall();
        //else
            MovePlayerXZ();

        //if (!isGrounded)
        //    transform.Translate(-9.81f * Vector3.up * Time.deltaTime);

        DoJumpLogic();

        currentSpeedPercentage = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);

        //ApplyDutchToCamera();
        //ApplyNoiseToCamera();

        //bool mousePressed = Mouse.current.leftButton.wasPressedThisFrame;
        //if (mousePressed && Physics.Raycast(mainCameraTransform.position, mainCameraTransform.forward, out RaycastHit hit) && !hit.collider.CompareTag("Player"))
        //{
        //    isHooking = true;
        //    hookPointRigidBody = Instantiate(hookPointPrefab, hit.point, Quaternion.identity).GetComponent<Rigidbody>();
        //    configurableJoint.connectedBody = hookPointRigidBody;
        //    configurableJoint.xMotion = ConfigurableJointMotion.Limited;
        //    configurableJoint.yMotion = ConfigurableJointMotion.Limited;
        //    configurableJoint.zMotion = ConfigurableJointMotion.Limited;
        //    SoftJointLimit limit = new SoftJointLimit();
        //    limit.limit = Vector3.Distance(mainCameraTransform.position, hookPointRigidBody.transform.position);
        //    configurableJoint.linearLimit = limit;
        //}

        //if (isHooking && isJumpPressed)
        //{
        //    isHooking = false;
        //    configurableJoint.connectedBody = null;
        //    configurableJoint.xMotion = ConfigurableJointMotion.Free;
        //    configurableJoint.yMotion = ConfigurableJointMotion.Free;
        //    configurableJoint.zMotion = ConfigurableJointMotion.Free;
        //    Destroy(hookPointRigidBody.gameObject);
        //}

        //bool mousePressedContinuously = Mouse.current.leftButton.isPressed;
        //if (mousePressedContinuously && isHooking)
        //{
        //    SoftJointLimit limit = new SoftJointLimit();
        //    limit.limit = Mathf.Max(0.1f, configurableJoint.linearLimit.limit - hookRewindForce * Time.deltaTime);
        //    configurableJoint.linearLimit = limit;

        //    currentSpeed += hookRewindSpeedAcceleration * Time.deltaTime;
        //    if (currentSpeed > maxSpeed)
        //        currentSpeed = maxSpeed;
        //}

    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 20), "Speed: " + currentSpeed);
        GUI.Label(new Rect(10, 30, 100, 20), "Grounded: " + isGrounded);
    }

    public void SetCanDoubleJump(bool canDoubleJump)
    {
        this.canDoubleJump = canDoubleJump;
    }

    private void ReadInput()
    {
        isJumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        isJumpPressedContinuously = Keyboard.current.spaceKey.isPressed;
        rawMoveInput = inputManager.Player.Move.ReadValue<Vector2>();
    }

    private void SmoothMoveInput()
    {
        smoothMoveInput.x = Mathf.SmoothDamp(smoothMoveInput.x, rawMoveInput.x, ref unusedCurrentVelocity1, inputSmoothTimer);
        smoothMoveInput.y = Mathf.SmoothDamp(smoothMoveInput.y, rawMoveInput.y, ref unusedCurrentVelocity2, inputSmoothTimer);
    }

    private void UpdatePlayerSpeed()
    {
        if (rawMoveInput.sqrMagnitude > 0.1f || !isGrounded)
            IncrementSpeed();
        else
            DecrementSpeed();
    }

    private void IncrementSpeed()
    {
        currentSpeed += speedAcceleration * Time.deltaTime;
        if (currentSpeed > maxSpeed)
            currentSpeed = maxSpeed;
    }

    private void DecrementSpeed()
    {
        currentSpeed -= speedDeceleration * Time.deltaTime;
        if (currentSpeed < minSpeed)
            currentSpeed = minSpeed;
    }

    private void CalculatePlayerDesiredXZDirection()
    {
        Vector3 camForwardXZNormalized = new Vector3(mainCameraTransform.forward.x, 0, mainCameraTransform.forward.z).normalized;
        Vector3 camRightXZNormalized = new Vector3(mainCameraTransform.right.x, 0, mainCameraTransform.right.z).normalized;
        Vector3 move = camForwardXZNormalized * smoothMoveInput.y + camRightXZNormalized * smoothMoveInput.x;

        playerDesiredXZDirection = currentSpeed * Time.deltaTime * move;
    }

    private void CheckCollisionWithWalls()
    {
        Vector3 capsulePoint1 = transform.position + Vector3.up * 0.5f;
        Vector3 capsulePoint2 = transform.position + Vector3.up * 1.5f;
        isCollidingWithWall = Physics.CapsuleCast(
            capsulePoint1,
            capsulePoint2,
            0.5f,
            playerDesiredXZDirection.normalized,
            out RaycastHit hit,
            playerDesiredXZDirection.magnitude + 0.5f);

        collisionWithWallNormal = hit.normal;
    }

    private void CheckCollisionWithGround()
    {
        Vector3 capsulePoint1 = transform.position + Vector3.up * 0.5f;
        Vector3 capsulePoint2 = transform.position + Vector3.up * 1.5f;
        isGrounded = Physics.CapsuleCast(
            capsulePoint1,
            capsulePoint2,
            0.5f,
            Vector3.down,
            10f * Time.deltaTime);
    }

    private void MovePlayerXZ()
    {
        //if (!isCollidingWithWall)
            characterController.Move(playerDesiredXZDirection);
        //else
        //{
        //    Vector3 newDirection = Vector3.Cross(collisionWithWallNormal, Vector3.up);
        //    if (Vector3.Dot(newDirection, playerDesiredXZDirection) < 0)
        //        newDirection = -newDirection;
        //    newDirection *= Vector3.Dot(newDirection, playerDesiredXZDirection);
        //    transform.Translate(newDirection);
        //    currentSpeed -= speedAcceleration * Time.deltaTime;
        //}
    }

    private void MovePlayerWhileRubbingOnWall()
    {
        Vector3 move = mainCameraTransform.forward * smoothMoveInput.y;
        Vector3 direction = currentSpeed * Time.deltaTime * move;

        Vector3 newDirection = Vector3.Cross(collisionWithWallNormal, Vector3.up);
        if (Vector3.Dot(newDirection, direction) < 0)
            newDirection = -newDirection;

        newDirection = Vector3.up * Vector3.Dot(Vector3.up, direction) + newDirection * Vector3.Dot(newDirection, direction);

        //rigidBody.MovePosition(rigidBody.position + newDirection);
    }

    private void DoJumpLogic()
    {
        if (isGrounded)
            canDoubleJump = true;

        if (isJumpPressed && (isGrounded || canDoubleJump))
        {
            playerVelocityY = jumpHeight;
            characterController.Move(playerVelocityY * Time.deltaTime * Vector3.up);
            //rigidBody.AddForce((jumpHeight - rigidBody.velocity.y) * Vector3.up, ForceMode.Impulse);
        }

        if (isJumpPressed && !isGrounded && canDoubleJump)
            canDoubleJump = false;
    }

    private void ApplyDutchToCamera()
    {
        currentDutchDuration = Mathf.Lerp(initialDutchDuration, finalDutchDuration, currentSpeedPercentage);
        dutchChangeTimer += Time.deltaTime;

        if (isGrounded && rawMoveInput.sqrMagnitude > 0.1f)
        {
            if (dutchChangeTimer >= currentDutchDuration)
            {
                float dutchAngle;
                if (currentSpeedPercentage < 0.85f)
                    dutchAngle = Mathf.Lerp(0, maxDutch, currentSpeedPercentage);
                else
                    dutchAngle = Mathf.Lerp(0, maxDutch, 0.5f);

                if (invertDutch)
                    dutchAngle = -dutchAngle;

                cinemachineVirtualCamera.m_Lens.Dutch = dutchAngle;
                invertDutch = !invertDutch;
                dutchChangeTimer = 0;
            }
        }
        else
            cinemachineVirtualCamera.m_Lens.Dutch = 0;
    }

    private void ApplyNoiseToCamera()
    {
        if (rawMoveInput.sqrMagnitude > 0.1f)
            cinemachineNoise.m_AmplitudeGain = Mathf.Lerp(0, maxCameraNoise, currentSpeedPercentage);
        else
            cinemachineNoise.m_AmplitudeGain = 0;
    }

}
