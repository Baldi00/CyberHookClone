using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneReloader : MonoBehaviour
{
    [SerializeField]
    private float pressAgainTimerDuration = 0.3f;

    private float pressAgainTimer = 0;

    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (pressAgainTimer > 0)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            else
                pressAgainTimer = pressAgainTimerDuration;
        }
        pressAgainTimer = Mathf.Max(0, pressAgainTimer - Time.deltaTime);
    }
}
