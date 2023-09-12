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
		Time.timeScale = 1f;

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
