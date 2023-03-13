using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
