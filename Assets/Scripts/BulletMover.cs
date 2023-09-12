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

    void Update()
    {
        Vector3 nextPosition = transform.position + bulletSpeed * Time.deltaTime * transform.forward;
        if (Physics.Raycast(transform.position, nextPosition - transform.position,
            out RaycastHit hit, Vector3.Distance(nextPosition, transform.position)) && !hit.collider.isTrigger)
        {
            if (hit.collider.CompareTag("DestroyableBlock"))
            {
                hit.collider.GetComponent<CubeDestroyer>().DestroyCube();
                DestroyImmediate(gameObject);
                return;
            }
            transform.forward = Vector3.Reflect(transform.forward, hit.normal);
        }

        transform.position = nextPosition;

        timer += Time.deltaTime;
        if (timer > maxLifetime)
            Destroy(gameObject);
    }
}
