using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
	private static GameManager _instance;
	public static GameManager Instance { get => _instance; }

	public DefaultInputActions inputManager;

	void Awake()
	{
		if (_instance == null)
			_instance = this;
		else
		{
			Destroy(gameObject);
			return;
		}

		inputManager = new DefaultInputActions();
		inputManager.Enable();
	}

}
