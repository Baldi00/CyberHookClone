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
    [SerializeField]
    private CinemachineVirtualCamera cinemachineVirtualCamera;

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
    private float inputSmoothTimer = 0.1f;
    [SerializeField]
    private float maxSpeed = 28f;
    [SerializeField]
    private float minSpeed = 5f;
    [SerializeField]
    private float jumpHeight = 1.0f;
    [SerializeField]
    private float gravityValue = -9.81f;
    [SerializeField]
    private float maxDutch = 1f;
    [SerializeField]
    private float initialDutchDuration = 0.5f;
    [SerializeField]
    private float finalDutchDuration = 0.25f;
    [SerializeField]
    private float maxCameraNoise = 1.5f;

    private bool canDoubleJump;
    private Vector2 smoothInputMove;
    private DefaultInputActions inputManager;
    private CharacterController characterController;
    private Transform mainCameraTransform;
    private Vector3 playerVelocity;
    private float currentSpeed;
    private bool isGrounded;
    private bool jumpPressed;
    private float currentDutchDuration;
    private float dutchChangeTimer;
    private bool invertDutch;
    private CinemachineBasicMultiChannelPerlin cinemachineNoise;

    private float unusedCurrentVelocity1;
    private float unusedCurrentVelocity2;

    void Start()
    {
        mainCameraTransform = Camera.main.transform;
        inputManager = GameManager.Instance.inputManager;
        characterController = GetComponent<CharacterController>();
        cinemachineNoise = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        currentSpeed = playerInitialSpeed;
        smoothInputMove = Vector2.zero;
        canDoubleJump = true;
    }

    void Update()
    {
        jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;

        isGrounded = characterController.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
            canDoubleJump = true;
        }

        Vector2 inputMove = inputManager.Player.Move.ReadValue<Vector2>();

        smoothInputMove.x = Mathf.SmoothDamp(smoothInputMove.x, inputMove.x, ref unusedCurrentVelocity1, inputSmoothTimer);
        smoothInputMove.y = Mathf.SmoothDamp(smoothInputMove.y, inputMove.y, ref unusedCurrentVelocity2, inputSmoothTimer);

        Vector3 camForwardXZNormalized = new Vector3(mainCameraTransform.forward.x, 0, mainCameraTransform.forward.z).normalized;
        Vector3 camRightXZNormalized = new Vector3(mainCameraTransform.right.x, 0, mainCameraTransform.right.z).normalized;
        Vector3 move = camForwardXZNormalized * smoothInputMove.y + camRightXZNormalized * smoothInputMove.x;

        if (inputMove.sqrMagnitude > 0.1f || !isGrounded)
            IncrementSpeed();
        else
            DecrementSpeed();

        characterController.Move(move * Time.deltaTime * currentSpeed);

        if (jumpPressed && (isGrounded || canDoubleJump))
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);

        if(jumpPressed && !isGrounded && canDoubleJump)
            canDoubleJump = false;

        playerVelocity.y += gravityValue * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);

        float currentSpeedPercentage = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);

        currentDutchDuration = Mathf.Lerp(initialDutchDuration, finalDutchDuration, currentSpeedPercentage);
        dutchChangeTimer += Time.deltaTime;

        if (isGrounded && inputMove.sqrMagnitude > 0.1f)
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

        if (inputMove.sqrMagnitude > 0.1f)
            cinemachineNoise.m_AmplitudeGain = Mathf.Lerp(0, maxCameraNoise, currentSpeedPercentage);
        else
            cinemachineNoise.m_AmplitudeGain = 0;
    }

    public void SetCanDoubleJump(bool canDoubleJump)
    {
        this.canDoubleJump = canDoubleJump;
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
}
