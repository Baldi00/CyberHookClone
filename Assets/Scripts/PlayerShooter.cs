using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField]
    private GameObject bulletPrefab;

    private Transform mainCameraTransform;

    void Awake()
    {
        mainCameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
            Instantiate(bulletPrefab, mainCameraTransform.position + mainCameraTransform.forward, mainCameraTransform.rotation);
    }
}
