using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ConfigurableJoint))]
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

    [Header("Jump & Gravity")]
    [SerializeField]
    private float jumpHeight = 1.0f;
    [SerializeField]
    private float gravityValue = -9.81f;

    [Header("Hook")]
    [SerializeField]
    private GameObject hookPointPrefab;
    [SerializeField]
    private float hookMaxDistance = 20f;
    [SerializeField]
    private float hookMaxDistanceBulletTime = 30f;
    [SerializeField]
    private float hookRewindForce = 20f;
    [SerializeField]
    private float hookRewindSpeedAcceleration = 3.5f;
    [Tooltip("After hook force is used to simulate the force of the hook when the hooking is over")]
    [SerializeField]
    private float afterHookForceDuration = 1f;
    [SerializeField]
    private LineRenderer hookLineRenderer;
    [SerializeField]
    private Transform handPositionTransform;
    [SerializeField]
    private Transform hookCrosshair;

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
    [SerializeField]
    private Material speedPostProcessEffectMaterial;

    private DefaultInputActions inputManager;
    private CharacterController characterController;
    private Rigidbody rigidBody;
    private ConfigurableJoint configurableJoint;
    private Transform mainCameraTransform;

    private bool isUsingRigidBody;

    private Vector2 rawMoveInput;
    private Vector2 smoothMoveInput;
    private Vector3 playerDesiredXZDirection;
    private Vector3 playerVelocity;
    private float currentSpeed;
    private float currentSpeedPercentage;

    private bool isGrounded;
    private bool isMousePressed;
    private bool isMousePressedContinuously;
    private bool isJumpPressed;
    private bool isJumpPressedContinuously;
    private float availableJumpsCount;

    private bool isCollidingWithWall;
    private Vector3 collisionWithWallNormal;
    private bool isRubbingAgainstWall;

    private CinemachineBasicMultiChannelPerlin cinemachineNoise;
    private float currentDutchDuration;
    private float dutchChangeTimer;
    private bool invertDutch;

    private bool isHooking;
    private Rigidbody hookPointRigidBody;
    private Vector3 afterHookForce;
    private float afterHookForceTimer;
    private float currentHookMaxDistance;

    private float unusedCurrentVelocity1;
    private float unusedCurrentVelocity2;

    void Awake()
    {
        mainCameraTransform = Camera.main.transform;
        characterController = GetComponent<CharacterController>();
        rigidBody = GetComponent<Rigidbody>();
        configurableJoint = GetComponent<ConfigurableJoint>();
        cinemachineNoise = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        currentSpeed = playerInitialSpeed;
        smoothMoveInput = Vector2.zero;
        availableJumpsCount = 2;

        currentHookMaxDistance = hookMaxDistance;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        // Called in Start cause GameManager.Instance is instantiated in Awake
        inputManager = GameManager.Instance.inputManager;
    }

    void Update()
    {
        ReadInput();
        SmoothMoveInput();
        DoPlayerGroundedCheck();
        UpdatePlayerSpeed();
        DoCollisionWithWallsCheck();
        ComputePlayerDesiredDirection();
        DoRubbingAgainstWallCheck();
        UpdateAfterHookForceTimer();
        UpdatePlayerPosition();
        UpdateHookCrosshair();
        DoHookLogic();
        DoJumpLogic();
        ApplyDutchToCamera();
        ApplyNoiseToCamera();
        ApplySpeedPostProcessEffect();
        UpdateHookLineRenderer();
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10f, 10f, 200f, 20f), "Speed: " + currentSpeed);
        GUI.Label(new Rect(10f, 30f, 200f, 20f), "Grounded: " + isGrounded);
        GUI.Label(new Rect(10f, 50f, 200f, 20f), "Jump count: " + availableJumpsCount);
        GUI.Label(new Rect(10f, 70f, 200f, 20f), "Wall collision: " + isCollidingWithWall);
        GUI.Label(new Rect(10f, 90f, 200f, 20f), "Rubbing: " + isRubbingAgainstWall);
        GUI.Label(new Rect(10f, 110f, 200f, 20f), "RB: " + isUsingRigidBody);
    }

    public void SetBulletTime(bool active)
    {
        currentHookMaxDistance = active ? hookMaxDistanceBulletTime : hookMaxDistance;
    }

    /// <summary>
    /// Reads input values from mouse and keyboard and sets the related parameters
    /// </summary>
    private void ReadInput()
    {
        isJumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        isJumpPressedContinuously = Keyboard.current.spaceKey.isPressed;
        isMousePressed = Mouse.current.leftButton.wasPressedThisFrame;
        isMousePressedContinuously = Mouse.current.leftButton.isPressed;
        rawMoveInput = inputManager.Player.Move.ReadValue<Vector2>();
    }

    /// <summary>
    /// Smooth the player move inputs in order to make little acceleration and deceleration
    /// </summary>
    private void SmoothMoveInput()
    {
        smoothMoveInput.x = Mathf.SmoothDamp(smoothMoveInput.x, rawMoveInput.x, ref unusedCurrentVelocity1, inputSmoothTimer);
        smoothMoveInput.y = Mathf.SmoothDamp(smoothMoveInput.y, rawMoveInput.y, ref unusedCurrentVelocity2, inputSmoothTimer);
    }

    /// <summary>
    /// Determines whether the player is grounded or not and if it is the case updates some parameters
    /// </summary>
    private void DoPlayerGroundedCheck()
    {
        if (isUsingRigidBody)
            isGrounded = DoCapsuleCast(Vector3.down, 1);
        else
            isGrounded = characterController.isGrounded;

        if (isGrounded && playerVelocity.y <= 0)
        {
            playerVelocity.y = 0;
            availableJumpsCount = 2;
        }
    }

    /// <summary>
    /// Performs a capsule cast in the given direction up to the maximum given distance
    /// </summary>
    /// <param name="direction">The direction in which the capsule cast is performed</param>
    /// <param name="distance">The maximum distance to reach with the capsule cast</param>
    /// <returns>True if the capsule cast collides with something, false otherwise</returns>
    private bool DoCapsuleCast(Vector3 direction, float distance)
    {
        RaycastHit unused;
        return DoCapsuleCast(direction, distance, out unused);
    }

    /// <summary>
    /// Performs a capsule cast in the given direction up to the maximum given distance
    /// </summary>
    /// <param name="direction">The direction in which the capsule cast is performed</param>
    /// <param name="distance">The maximum distance to reach with the capsule cast</param>
    /// <param name="hit">Filled with the information of the hit in case capsule cast finds something</param>
    /// <returns>True if the capsule cast collides with something, false otherwise</returns>
    private bool DoCapsuleCast(Vector3 direction, float distance, out RaycastHit hit)
    {
        hit = new RaycastHit();
        Vector3 capsulePoint1 = transform.position + Vector3.up * 0.5f;
        Vector3 capsulePoint2 = transform.position + Vector3.up * 1.5f;
        return Physics.CapsuleCast(capsulePoint1, capsulePoint2, 0.5f - Physics.defaultContactOffset, direction, out hit, distance) &&
            !hit.collider.CompareTag("Player");
    }

    /// <summary>
    /// Updates the current player speed.
    /// It increases speed if the player is moving or if it is falling (not grounded), otherwise decreases it
    /// </summary>
    private void UpdatePlayerSpeed()
    {
        if (IsPlayerMoving() || !isGrounded)
            IncrementSpeed();
        else
            DecrementSpeed();

        currentSpeedPercentage = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
    }

    /// <summary>
    /// Checks if the player is moving
    /// </summary>
    /// <returns>True if the player is moving, false otherwise</returns>
    private bool IsPlayerMoving()
    {
        return (!isUsingRigidBody && playerVelocity.sqrMagnitude > 0.1f) ||
            (isUsingRigidBody && rigidBody.velocity.sqrMagnitude > 0.1f) ||
            (isHooking && isMousePressedContinuously);
    }

    /// <summary>
    /// Increases current speed at a given rate, stops when reaches the max speed
    /// </summary>
    private void IncrementSpeed()
    {
        currentSpeed += speedAcceleration * Time.deltaTime;
        if (currentSpeed > maxSpeed)
            currentSpeed = maxSpeed;
    }

    /// <summary>
    /// Decreases current speed at a given rate, stops when reaches the minimum speed
    /// </summary>
    private void DecrementSpeed()
    {
        currentSpeed -= speedDeceleration * Time.deltaTime;
        if (currentSpeed < minSpeed)
            currentSpeed = minSpeed;
    }

    /// <summary>
    /// Checks if the player is colliding with a wall and sets related parameters
    /// </summary>
    private void DoCollisionWithWallsCheck()
    {
        float distance = playerDesiredXZDirection.magnitude + 0.5f;
        isCollidingWithWall = DoCapsuleCast(playerDesiredXZDirection.normalized, distance, out RaycastHit hit);

        if (isCollidingWithWall)
            collisionWithWallNormal = hit.normal;

        if (isCollidingWithWall && !isGrounded)
            availableJumpsCount = 1;
    }

    /// <summary>
    /// Computes the direction the player wants to go to
    /// </summary>
    private void ComputePlayerDesiredDirection()
    {
        Vector3 camForwardXZNormalized = new Vector3(mainCameraTransform.forward.x, 0, mainCameraTransform.forward.z).normalized;
        Vector3 camRightXZNormalized = new Vector3(mainCameraTransform.right.x, 0, mainCameraTransform.right.z).normalized;
        Vector3 move = camForwardXZNormalized * smoothMoveInput.y + camRightXZNormalized * smoothMoveInput.x;
        playerVelocity.x = move.x;
        playerVelocity.z = move.z;

        playerDesiredXZDirection = currentSpeed * Time.deltaTime * move;
    }

    /// <summary>
    /// Checks if the player is rubbing against wall.
    /// It is rubbing if it is colliding with a wall, watching at the wall and pressing the related button
    /// </summary>
    private void DoRubbingAgainstWallCheck()
    {
        isRubbingAgainstWall =
            isCollidingWithWall &&
            isJumpPressedContinuously &&
            Vector3.Dot(playerDesiredXZDirection, collisionWithWallNormal) < 0;
    }

    /// <summary>
    /// Updates the timer for the after hook force to apply.
    /// After hook force is used to simulate the force of the hook when the hooking is over
    /// </summary>
    private void UpdateAfterHookForceTimer()
    {
        afterHookForceTimer += Time.deltaTime;
        if (afterHookForceTimer > afterHookForceDuration)
            afterHookForceTimer = afterHookForceDuration;
    }

    /// <summary>
    /// Moves the player position in the world
    /// </summary>
    private void UpdatePlayerPosition()
    {
        if (isRubbingAgainstWall)
            MovePlayerWhileRubbingOnWall();
        else
            MovePlayerOnXZ();
    }

    /// <summary>
    /// Moves the player position as if it is climbing a wall.
    /// The player goes in the direction the camera is pointing to
    /// </summary>
    private void MovePlayerWhileRubbingOnWall()
    {
        Vector3 move = mainCameraTransform.forward * smoothMoveInput.y;
        Vector3 direction = currentSpeed * Time.deltaTime * move;

        Vector3 newDirection = Vector3.Cross(collisionWithWallNormal, Vector3.up);
        if (Vector3.Dot(newDirection, direction) < 0)
            newDirection = -newDirection;

        newDirection = Vector3.up * Vector3.Dot(Vector3.up, direction) + newDirection * Vector3.Dot(newDirection, direction);

        if (!isUsingRigidBody)
            characterController.Move(newDirection);
        else
            rigidBody.AddForce(newDirection, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Moves the player position on the XZ plane.
    /// It moves it even if the player isn't grounded
    /// </summary>
    private void MovePlayerOnXZ()
    {
        if (!isUsingRigidBody)
        {
            Vector3 currentAfterHookForce = Vector3.Lerp(afterHookForce, Vector3.zero, afterHookForceTimer / afterHookForceDuration);
            characterController.Move(playerDesiredXZDirection + currentAfterHookForce * Time.deltaTime);
        }
        else
            rigidBody.AddForce(playerDesiredXZDirection, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Updates the hook crosshair according to the distance of the closest hookable object
    /// </summary>
    private void UpdateHookCrosshair()
    {
        if (Physics.Raycast(
            mainCameraTransform.position + mainCameraTransform.forward,
            mainCameraTransform.forward, out RaycastHit hit, currentHookMaxDistance)
            && hit.collider.CompareTag("Hookable"))
        {
            float distance = hit.distance;
            if (distance <= currentHookMaxDistance)
            {
                float scale = Mathf.InverseLerp(0, currentHookMaxDistance, distance);
                hookCrosshair.gameObject.SetActive(true);
                hookCrosshair.localScale = Vector3.one * scale;
            }
            else
                hookCrosshair.gameObject.SetActive(false);
        }
        else
            hookCrosshair.gameObject.SetActive(false);
    }

    /// <summary>
    /// Performs the checks and actions to make the hook work.
    /// Checks and actuates the start, stop and rewind actions
    /// </summary>
    private void DoHookLogic()
    {
        if (IsPlayerStartingHooking(out RaycastHit hit))
            StartHooking(hit);

        if (IsPlayerStoppingHooking())
            StopHooking();

        if (IsPlayerRewindingHook())
        {
            rigidBody.useGravity = false;
            RewindHook();
        }
        else
            rigidBody.useGravity = true;
    }

    /// <summary>
    /// Checks whether or not the player has started hooking
    /// Hooking starts when there's a hookable object near enouth,
    /// the player is pointing to it, it presses the correct button and it wasn't already hooking
    /// </summary>
    /// <returns>True if the player has started hooking, false otherwise</returns>
    private bool IsPlayerStartingHooking(out RaycastHit hit)
    {
        hit = new RaycastHit();
        return isMousePressed &&
            Physics.Raycast(mainCameraTransform.position + mainCameraTransform.forward, mainCameraTransform.forward, out hit, currentHookMaxDistance) &&
            hit.collider.CompareTag("Hookable") &&
            !isHooking;
    }

    /// <summary>
    /// Checks whether or not the player has stopped hooking
    /// Hooking ends when the player was hooking and the correct button is pressed
    /// </summary>
    /// <returns>True if the player has stopped hooking, false otherwise</returns>
    private bool IsPlayerStoppingHooking()
    {
        return isHooking && isJumpPressed;
    }

    /// <summary>
    /// Checks whether or not the player is rewinding hook
    /// Hook rewinds when the player is hooking and the correct button is pressed
    /// </summary>
    /// <returns>True if the player is rewinding hook, false otherwise</returns>
    private bool IsPlayerRewindingHook()
    {
        return isMousePressedContinuously && isHooking;
    }

    /// <summary>
    /// Enables the hooking state
    /// </summary>
    /// <param name="hit">The collision point the hook will grab on</param>
    private void StartHooking(RaycastHit hit)
    {
        isHooking = true;
        availableJumpsCount = 2;

        rigidBody.isKinematic = false;
        rigidBody.velocity = Vector3.zero;
        characterController.enabled = false;
        isUsingRigidBody = isHooking;

        hookPointRigidBody = Instantiate(hookPointPrefab, hit.point, Quaternion.identity).GetComponent<Rigidbody>();
        float distanceFromHookPoint = Vector3.Distance(mainCameraTransform.position, hookPointRigidBody.transform.position);

        SetJointConnectedBody(hookPointRigidBody);
        SetJointMotion(ConfigurableJointMotion.Limited);
        SetJointLimit(distanceFromHookPoint);
    }

    /// <summary>
    /// Disables the hooking state
    /// </summary>
    private void StopHooking()
    {
        isHooking = false;

        if (!isMousePressedContinuously)
            afterHookForce = Vector3.zero;
        afterHookForceTimer = 0;

        rigidBody.isKinematic = false;
        characterController.enabled = true;
        isUsingRigidBody = isHooking;

        SetJointConnectedBody(null);
        SetJointMotion(ConfigurableJointMotion.Free);
        Destroy(hookPointRigidBody.gameObject);
    }

    /// <summary>
    /// Rewinds the hook at a set speed and increases player speed
    /// </summary>
    private void RewindHook()
    {
        float newLimit = Mathf.Max(0.1f, configurableJoint.linearLimit.limit - hookRewindForce * Time.deltaTime);
        SetJointLimit(newLimit);

        afterHookForce = (hookPointRigidBody.transform.position - transform.position).normalized * hookRewindForce;

        currentSpeed += hookRewindSpeedAcceleration * Time.deltaTime;
        if (currentSpeed > maxSpeed)
            currentSpeed = maxSpeed;
    }

    /// <summary>
    /// Sets the connected rigidbody of the configurable joint for the hook effect
    /// </summary>
    private void SetJointConnectedBody(Rigidbody rigidbody)
    {
        configurableJoint.connectedBody = rigidbody;
    }

    /// <summary>
    /// Sets the xyz motion state of the configurable joint for the hook effect
    /// </summary>
    private void SetJointMotion(ConfigurableJointMotion motion)
    {
        configurableJoint.xMotion = motion;
        configurableJoint.yMotion = motion;
        configurableJoint.zMotion = motion;
    }

    /// <summary>
    /// Sets the max distance limit of the configurable joint for the hook effect
    /// </summary>
    private void SetJointLimit(float limit)
    {
        configurableJoint.linearLimit = new SoftJointLimit() { limit = limit };
    }

    /// <summary>
    /// Performs checks and actuates actions in order to make the player jump.
    /// It also applies gravity when player is controlled by character controller.
    /// Player can jump when it is grounded or when it has double jump available.
    /// </summary>
    private void DoJumpLogic()
    {
        if (isJumpPressed && (isGrounded || availableJumpsCount > 0))
        {
            if (!isHooking)
                playerVelocity.y = jumpHeight;
            availableJumpsCount--;
        }

        if (!isUsingRigidBody && !isRubbingAgainstWall)
        {
            ApplyGravity();
            characterController.Move(playerVelocity.y * Time.deltaTime * Vector3.up);
        }
    }

    /// <summary>
    /// Applies gravity to the player velocity
    /// </summary>
    private void ApplyGravity()
    {
        playerVelocity.y += gravityValue * Time.deltaTime;
    }

    /// <summary>
    /// Applies a dutch angle to the camera according to the player current speed if it is grounded.
    /// This is to simulate player running steps.
    /// </summary>
    private void ApplyDutchToCamera()
    {
        currentDutchDuration = Mathf.Lerp(initialDutchDuration, finalDutchDuration, currentSpeedPercentage);
        dutchChangeTimer += Time.deltaTime;

        if (isGrounded && IsPlayerMoving())
        {
            if (dutchChangeTimer >= currentDutchDuration)
            {
                float dutchAngle = Mathf.Lerp(0, maxDutch, currentSpeedPercentage);
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

    /// <summary>
    /// Applies a radial post process effect according to the player current speed.
    /// </summary>
    private void ApplySpeedPostProcessEffect()
    {
        speedPostProcessEffectMaterial.SetFloat("_Speed", currentSpeedPercentage);
    }

    /// <summary>
    /// Applies a noise to the camera according to the player current speed.
    /// This is a visual effect to represent player speed.
    /// </summary>
    private void ApplyNoiseToCamera()
    {
        if (IsPlayerMoving())
            cinemachineNoise.m_AmplitudeGain = Mathf.Lerp(0, maxCameraNoise, currentSpeedPercentage);
        else
            cinemachineNoise.m_AmplitudeGain = 0;
    }

    /// <summary>
    /// Enables or disables a line renderer from the player hand to the hook grab point to simulate a real hook rope
    /// </summary>
    private void UpdateHookLineRenderer()
    {
        if (isHooking)
        {
            hookLineRenderer.enabled = true;
            hookLineRenderer.SetPosition(0, handPositionTransform.position);
            hookLineRenderer.SetPosition(1, hookPointRigidBody.transform.position);
        }
        else
            hookLineRenderer.enabled = false;
    }

}
