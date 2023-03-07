using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
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

    private DefaultInputActions inputManager;
    private Rigidbody rigidBody;
    private Transform mainCameraTransform;

    private Vector2 rawMoveInput;
    private Vector2 smoothMoveInput;
    private Vector3 playerDesiredXZDirection;
    private float currentSpeed;
    private float currentSpeedPercentage;
    private bool isGrounded;
    private bool jumpPressed;
    private bool jumpPressedContinuously;
    private bool canDoubleJump;

    private Vector3 collisionWithWallNormal;
    private bool collidingWithWall;
    private bool isRubbingAgainstWall;

    private CinemachineBasicMultiChannelPerlin cinemachineNoise;
    private float currentDutchDuration;
    private float dutchChangeTimer;
    private bool invertDutch;

    private float unusedCurrentVelocity1;
    private float unusedCurrentVelocity2;

    void Awake()
    {
        mainCameraTransform = Camera.main.transform;
        rigidBody = GetComponent<Rigidbody>();
        cinemachineNoise = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        currentSpeed = playerInitialSpeed;
        smoothMoveInput = Vector2.zero;
        canDoubleJump = true;
    }

    void Start()
    {
        inputManager = GameManager.Instance.inputManager;
    }

    void Update()
    {
        ReadInput();
        SmoothMoveInput();

        isGrounded = Physics.Raycast(transform.position + Vector3.up, Vector3.down, 1.01f);

        UpdatePlayerSpeed();

        CalculatePlayerDesiredXZDirection();
        CheckCollisionWithWalls();

        if (collidingWithWall)
            canDoubleJump = true;

        isRubbingAgainstWall = collidingWithWall && jumpPressedContinuously;

        if (isRubbingAgainstWall)
        {
            rigidBody.useGravity = false;
            MovePlayerWhileRubbingOnWall();
        }
        else
        {
            rigidBody.useGravity = true;
            MovePlayerXZ();
        }

        DoJumpLogic();

        currentSpeedPercentage = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);

        ApplyDutchToCamera();
        ApplyNoiseToCamera();

    }

    public void SetCanDoubleJump(bool canDoubleJump)
    {
        this.canDoubleJump = canDoubleJump;
    }

    private void ReadInput()
    {
        jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        jumpPressedContinuously = Keyboard.current.spaceKey.isPressed;
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
        Vector3 capsulePoint1 = rigidBody.position + Vector3.up * 0.5f;
        Vector3 capsulePoint2 = rigidBody.position + Vector3.up * 1.5f;
        collidingWithWall = Physics.CapsuleCast(
            capsulePoint1,
            capsulePoint2,
            0.5f,
            playerDesiredXZDirection,
            out RaycastHit hit,
            playerDesiredXZDirection.magnitude + 0.5f);

        collisionWithWallNormal = hit.normal;
    }

    private void MovePlayerXZ()
    {
        if (!collidingWithWall)
            rigidBody.MovePosition(rigidBody.position + playerDesiredXZDirection);
        else
        {
            Vector3 newDirection = Vector3.Cross(collisionWithWallNormal, Vector3.up);
            if (Vector3.Dot(newDirection, playerDesiredXZDirection) < 0)
                newDirection = -newDirection;
            newDirection *= Vector3.Dot(newDirection, playerDesiredXZDirection);
            rigidBody.MovePosition(rigidBody.position + newDirection);
            currentSpeed -= speedAcceleration * Time.deltaTime;
        }
    }

    private void MovePlayerWhileRubbingOnWall()
    {
        Vector3 move = mainCameraTransform.forward * smoothMoveInput.y;
        Vector3 direction = currentSpeed * Time.deltaTime * move;

        Vector3 newDirection = Vector3.Cross(collisionWithWallNormal, Vector3.up);
        if (Vector3.Dot(newDirection, direction) < 0)
            newDirection = -newDirection;

        newDirection = Vector3.up * Vector3.Dot(Vector3.up, direction) + newDirection * Vector3.Dot(newDirection, direction);

        rigidBody.MovePosition(rigidBody.position + newDirection);
    }

    private void DoJumpLogic()
    {
        if (isGrounded)
            canDoubleJump = true;

        if (jumpPressed && (isGrounded || canDoubleJump))
            rigidBody.AddForce((jumpHeight - rigidBody.velocity.y) * Vector3.up, ForceMode.Impulse);

        if (jumpPressed && !isGrounded && canDoubleJump)
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
        if (rawMoveInput.sqrMagnitude > 0.1f)
            cinemachineNoise.m_AmplitudeGain = Mathf.Lerp(0, maxCameraNoise, currentSpeedPercentage);
        else
            cinemachineNoise.m_AmplitudeGain = 0;
    }

}
