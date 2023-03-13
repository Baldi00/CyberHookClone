using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;

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
    private int maxJumps = 2;
    [SerializeField]
    private float gravityValue = -9.81f;

    [Header("Hook")]
    [SerializeField]
    private GameObject hookPointPrefab;
    [SerializeField]
    private float hookMaxDistance = 20f;
    [SerializeField]
    private float hookRewindForce = 20f;
    [SerializeField]
    private float hookRewindSpeedAcceleration = 3.5f;
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
        // TODO: Remove from here
        // Reload scene for testing
        if (Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        ReadInput();
        SmoothMoveInput();
        CheckIfPlayerIsGrounded();

        if (isGrounded && playerVelocity.y <= 0)
        {
            playerVelocity.y = 0;
            availableJumpsCount = 2;
        }

        UpdatePlayerSpeed();
        currentSpeedPercentage = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);

        CheckCollisionWithWalls();
        if (isCollidingWithWall && !isGrounded)
            availableJumpsCount = 1;

        ComputePlayerDesiredDirection();
        CheckIfRubbingAgainstWall();

        afterHookForceTimer += Time.deltaTime;
        if (afterHookForceTimer > afterHookForceDuration)
            afterHookForceTimer = afterHookForceDuration;

        if (isRubbingAgainstWall)
            MovePlayerWhileRubbingOnWall();
        else
            MovePlayerOnXZ();

        UpdateHookCrosshair();

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

        DoJumpLogic();

        ApplyDutchToCamera();
        ApplyNoiseToCamera();
    }

    void LateUpdate()
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

    void OnGUI()
    {
        GUI.Label(new Rect(10f, 10f, 200f, 20f), "Speed: " + currentSpeed);
        GUI.Label(new Rect(10f, 30f, 200f, 20f), "Grounded: " + isGrounded);
        GUI.Label(new Rect(10f, 50f, 200f, 20f), "Jump count: " + availableJumpsCount);
        GUI.Label(new Rect(10f, 70f, 200f, 20f), "Wall collision: " + isCollidingWithWall);
        GUI.Label(new Rect(10f, 90f, 200f, 20f), "Rubbing: " + isRubbingAgainstWall);
        GUI.Label(new Rect(10f, 110f, 200f, 20f), "RB: " + isUsingRigidBody);
    }

    private void CheckIfPlayerIsGrounded()
    {
        if (isUsingRigidBody)
            isGrounded = DoCapsuleCast(Vector3.down, 1);
        else
            isGrounded = characterController.isGrounded;
    }

    private void RewindHook()
    {
        float newLimit = Mathf.Max(0.1f, configurableJoint.linearLimit.limit - hookRewindForce * Time.deltaTime);
        SetJointLimit(newLimit);

        afterHookForce = (hookPointRigidBody.transform.position - transform.position).normalized * hookRewindForce;

        currentSpeed += hookRewindSpeedAcceleration * Time.deltaTime;
        if (currentSpeed > maxSpeed)
            currentSpeed = maxSpeed;
    }

    private bool IsPlayerRewindingHook()
    {
        return isMousePressedContinuously && isHooking;
    }

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

    private bool IsPlayerStoppingHooking()
    {
        return isHooking && isJumpPressed;
    }

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

    private void CheckCollisionWithWalls()
    {
        float distance = playerDesiredXZDirection.magnitude + 0.5f;
        isCollidingWithWall = DoCapsuleCast(playerDesiredXZDirection.normalized, distance, out RaycastHit hit);
        if (isCollidingWithWall)
            collisionWithWallNormal = hit.normal;
    }

    private bool DoCapsuleCast(Vector3 direction, float distance, out RaycastHit hit)
    {
        hit = new RaycastHit();
        Vector3 capsulePoint1 = transform.position + Vector3.up * 0.5f;
        Vector3 capsulePoint2 = transform.position + Vector3.up * 1.5f;
        return Physics.CapsuleCast(capsulePoint1, capsulePoint2, 0.5f - Physics.defaultContactOffset, direction, out hit, distance) &&
            !hit.collider.CompareTag("Player");
    }

    private bool DoCapsuleCast(Vector3 direction, float distance)
    {
        RaycastHit unused;
        return DoCapsuleCast(direction, distance, out unused);
    }

    private void ReadInput()
    {
        isJumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        isJumpPressedContinuously = Keyboard.current.spaceKey.isPressed;
        isMousePressed = Mouse.current.leftButton.wasPressedThisFrame;
        isMousePressedContinuously = Mouse.current.leftButton.isPressed;
        rawMoveInput = inputManager.Player.Move.ReadValue<Vector2>();
    }

    private void SmoothMoveInput()
    {
        smoothMoveInput.x = Mathf.SmoothDamp(smoothMoveInput.x, rawMoveInput.x, ref unusedCurrentVelocity1, inputSmoothTimer);
        smoothMoveInput.y = Mathf.SmoothDamp(smoothMoveInput.y, rawMoveInput.y, ref unusedCurrentVelocity2, inputSmoothTimer);
    }

    private void UpdatePlayerSpeed()
    {
        if (IsPlayerMoving() || !isGrounded)
            IncrementSpeed();
        else
            DecrementSpeed();
    }

    private bool IsPlayerMoving()
    {
        return (!isUsingRigidBody && playerVelocity.sqrMagnitude > 0.1f) ||
            (isUsingRigidBody && rigidBody.velocity.sqrMagnitude > 0.1f) ||
            (isHooking && isMousePressedContinuously);
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

    private void ComputePlayerDesiredDirection()
    {
        Vector3 camForwardXZNormalized = new Vector3(mainCameraTransform.forward.x, 0, mainCameraTransform.forward.z).normalized;
        Vector3 camRightXZNormalized = new Vector3(mainCameraTransform.right.x, 0, mainCameraTransform.right.z).normalized;
        Vector3 move = camForwardXZNormalized * smoothMoveInput.y + camRightXZNormalized * smoothMoveInput.x;
        playerVelocity.x = move.x;
        playerVelocity.z = move.z;

        playerDesiredXZDirection = currentSpeed * Time.deltaTime * move;
    }

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

    private void CheckIfRubbingAgainstWall()
    {
        isRubbingAgainstWall =
            isCollidingWithWall &&
            isJumpPressedContinuously &&
            Vector3.Dot(playerDesiredXZDirection, collisionWithWallNormal) < 0;
    }

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

    private void ApplyGravity()
    {
        playerVelocity.y += gravityValue * Time.deltaTime;
    }

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

    private void ApplyNoiseToCamera()
    {
        if (IsPlayerMoving())
            cinemachineNoise.m_AmplitudeGain = Mathf.Lerp(0, maxCameraNoise, currentSpeedPercentage);
        else
            cinemachineNoise.m_AmplitudeGain = 0;
    }

    private void SetJointConnectedBody(Rigidbody rigidbody)
    {
        configurableJoint.connectedBody = rigidbody;
    }

    private void SetJointMotion(ConfigurableJointMotion motion)
    {
        configurableJoint.xMotion = motion;
        configurableJoint.yMotion = motion;
        configurableJoint.zMotion = motion;
    }

    private void SetJointLimit(float limit)
    {
        configurableJoint.linearLimit = new SoftJointLimit() { limit = limit };
    }

    private void AddJump()
    {
        if (availableJumpsCount < maxJumps)
            availableJumpsCount++;
    }

    private bool IsPlayerStartingHooking(out RaycastHit hit)
    {
        hit = new RaycastHit();
        return isMousePressed &&
            Physics.Raycast(mainCameraTransform.position + mainCameraTransform.forward, mainCameraTransform.forward, out hit, hookMaxDistance) &&
            !hit.collider.CompareTag("Player") &&
            !isHooking;
    }

    private void UpdateHookCrosshair()
    {
        if (Physics.Raycast(mainCameraTransform.position + mainCameraTransform.forward, mainCameraTransform.forward, out RaycastHit hit, hookMaxDistance))
        {
            float distance = hit.distance;
            if (distance <= hookMaxDistance)
            {
                float scale = Mathf.InverseLerp(0, hookMaxDistance, distance);
                hookCrosshair.gameObject.SetActive(true);
                hookCrosshair.localScale = Vector3.one * scale;
            }
            else
                hookCrosshair.gameObject.SetActive(false);
        }
        else
            hookCrosshair.gameObject.SetActive(false);
    }
}
