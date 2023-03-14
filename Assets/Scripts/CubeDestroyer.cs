using UnityEngine;

/// <summary>
/// Destroys this cube if it collides with a bullet and spawns a particle effect of the destruction
/// </summary>
public class CubeDestroyer : MonoBehaviour
{
    [SerializeField]
    private GameObject cubeDestroyEffectPrefab;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            ParticleSystem ps = Instantiate(cubeDestroyEffectPrefab, transform.position, Quaternion.identity).GetComponent<ParticleSystem>();
            Destroy(gameObject);
            Destroy(collision.gameObject);
        }

    }
}
