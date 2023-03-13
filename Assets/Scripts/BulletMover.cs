using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
