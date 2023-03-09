using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class TESTPHY : MonoBehaviour
{
	public Rigidbody rb;

	void Start()
	{
	}

	void Update()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame)
            rb.AddForce(Vector3.forward, ForceMode.Impulse);
        if (Keyboard.current.vKey.wasPressedThisFrame)
            rb.AddForce(Vector3.forward, ForceMode.VelocityChange);
        if (Keyboard.current.aKey.wasPressedThisFrame)
            rb.AddForce(Vector3.forward/Time.fixedDeltaTime, ForceMode.Force);
    }
}
