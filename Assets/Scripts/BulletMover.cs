using UnityEngine;

/// <summary>
/// Moves the bullet forward at a set speed
/// </summary>
public class BulletMover : MonoBehaviour
{
    [SerializeField]
    private float bulletSpeed = 20f;
    [SerializeField]
    private float maxLifetime = 5f;

    private float timer = 0;
    private Rigidbody rigidBody;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        rigidBody.position += bulletSpeed * Time.deltaTime * transform.forward;

        timer += Time.deltaTime;
        if (timer > maxLifetime)
            Destroy(gameObject);
    }
}
