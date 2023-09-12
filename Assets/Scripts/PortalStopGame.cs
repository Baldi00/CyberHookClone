using UnityEngine;

public class PortalStopGame : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SoundManager.Instance.PlayVictory();
            Time.timeScale = 0;
        }
    }
}
