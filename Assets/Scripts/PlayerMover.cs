using Cinemachine;
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
    private float gravityValue = -9.81f;

    [Header("Hook")]
    [SerializeField]
    private GameObject hookPointPrefab;
    [SerializeField]
    private float hookRewindForce = 20f;
    [SerializeField]
    private float hookRewindSpeedAcceleration = 3.5f;
    [SerializeField]
    private LineRenderer hookLineRenderer;
    [SerializeField]
    private Transform handPositionTransform;

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

    private bool isHooking;
    private Rigidbody hookPointRigidBody;

    private bool isUsingRigidBody;

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
        if (Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        ReadInput();
        SmoothMoveInput();

        isGrounded = characterController.isGrounded;

        UpdatePlayerSpeed();
        MovePlayerXZ();
        DoJumpLogic();

        currentSpeedPercentage = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);

        ApplyDutchToCamera();
        ApplyNoiseToCamera();

        bool mousePressed = Mouse.current.leftButton.wasPressedThisFrame;
        if (mousePressed && Physics.Raycast(mainCameraTransform.position, mainCameraTransform.forward, out RaycastHit hit) && !hit.collider.CompareTag("Player"))
        {
            isHooking = true;

            rigidBody.isKinematic = false;
            rigidBody.velocity = Vector3.zero;
            characterController.enabled = false;
            isUsingRigidBody = true;

            hookPointRigidBody = Instantiate(hookPointPrefab, hit.point, Quaternion.identity).GetComponent<Rigidbody>();
            configurableJoint.connectedBody = hookPointRigidBody;
            configurableJoint.xMotion = ConfigurableJointMotion.Limited;
            configurableJoint.yMotion = ConfigurableJointMotion.Limited;
            configurableJoint.zMotion = ConfigurableJointMotion.Limited;
            SoftJointLimit limit = new SoftJointLimit();
            limit.limit = Vector3.Distance(mainCameraTransform.position, hookPointRigidBody.transform.position);
            configurableJoint.linearLimit = limit;
        }

        if (isHooking && jumpPressed)
        {
            isHooking = false;

            rigidBody.isKinematic = false;
            characterController.enabled = true;
            isUsingRigidBody = false;

            configurableJoint.connectedBody = null;
            configurableJoint.xMotion = ConfigurableJointMotion.Free;
            configurableJoint.yMotion = ConfigurableJointMotion.Free;
            configurableJoint.zMotion = ConfigurableJointMotion.Free;
            Destroy(hookPointRigidBody.gameObject);
        }

        bool mousePressedContinuously = Mouse.current.leftButton.isPressed;
        if (mousePressedContinuously && isHooking)
        {
            SoftJointLimit limit = new SoftJointLimit();
            limit.limit = Mathf.Max(0.1f, configurableJoint.linearLimit.limit - hookRewindForce * Time.deltaTime);
            configurableJoint.linearLimit = limit;

            currentSpeed += hookRewindSpeedAcceleration * Time.deltaTime;
            if (currentSpeed > maxSpeed)
                currentSpeed = maxSpeed;
        }
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
        {
            hookLineRenderer.enabled = false;
        }
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

        if (!isUsingRigidBody)
            characterController.Move(currentSpeed * Time.deltaTime * move);
        else
            rigidBody.AddForce(currentSpeed * Time.deltaTime * move, ForceMode.VelocityChange);
    }

    private void DoJumpLogic()
    {
        if (isGrounded)
            canDoubleJump = true;

        if (jumpPressed && (isGrounded || canDoubleJump))
            playerVelocity.y = jumpHeight;

        if (!isUsingRigidBody)
        {
            ApplyGravity();
            characterController.Move(playerVelocity.y * Time.deltaTime * Vector3.up);
        }

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
