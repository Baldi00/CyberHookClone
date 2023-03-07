using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

[DisallowMultipleComponent]
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

    [Header("Jump & Gravity")]
    [SerializeField]
    private float jumpHeight = 1.0f;
    [SerializeField]
    private float gravityValue = -9.81f;

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
    private Transform mainCameraTransform;

    private Vector2 rawMoveInput;
    private Vector2 smoothMoveInput;
    private Vector3 playerVelocity;
    private float currentSpeed;
    private float currentSpeedPercentage;
    private bool isGrounded;
    private bool jumpPressed;
    private bool canDoubleJump;

    private CinemachineBasicMultiChannelPerlin cinemachineNoise;
    private float currentDutchDuration;
    private float dutchChangeTimer;
    private bool invertDutch;

    private float unusedCurrentVelocity1;
    private float unusedCurrentVelocity2;

    void Awake()
    {
        mainCameraTransform = Camera.main.transform;
        characterController = GetComponent<CharacterController>();
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

        isGrounded = characterController.isGrounded;

        UpdatePlayerSpeed();
        MovePlayerXZ();
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

    private void MovePlayerXZ()
    {
        Vector3 camForwardXZNormalized = new Vector3(mainCameraTransform.forward.x, 0, mainCameraTransform.forward.z).normalized;
        Vector3 camRightXZNormalized = new Vector3(mainCameraTransform.right.x, 0, mainCameraTransform.right.z).normalized;
        Vector3 move = camForwardXZNormalized * smoothMoveInput.y + camRightXZNormalized * smoothMoveInput.x;
        playerVelocity.x = move.x;
        playerVelocity.z = move.z;
        characterController.Move(currentSpeed * Time.deltaTime * move);
    }

    private void DoJumpLogic()
    {
        if (isGrounded)
            canDoubleJump = true;

        if (jumpPressed && (isGrounded || canDoubleJump))
            playerVelocity.y = jumpHeight;

        ApplyGravity();
        characterController.Move(playerVelocity.y * Time.deltaTime * Vector3.up);

        if (jumpPressed && !isGrounded && canDoubleJump)
            canDoubleJump = false;
    }

    private void ApplyGravity()
    {
        playerVelocity.y += gravityValue * Time.deltaTime;
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
