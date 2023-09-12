using UnityEngine;

/// <summary>
/// Destroys this cube if it collides with a bullet and spawns a particle effect of the destruction
/// </summary>
public class CubeDestroyer : MonoBehaviour
{
    [SerializeField]
    private GameObject cubeDestroyEffectPrefab;

    public void DestroyCube()
    {
        ParticleSystem ps = Instantiate(cubeDestroyEffectPrefab, transform.position, Quaternion.identity).GetComponent<ParticleSystem>();
        Destroy(gameObject);
    }
}
