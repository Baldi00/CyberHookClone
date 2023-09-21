using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalStopGame : MonoBehaviour
{
    [SerializeField]
    private Material speedPostProcessEffect;
    [SerializeField]
    private PlayerMover playerMover;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerMover.enabled = false;
            SoundManager.Instance.StopBackgroundMusic();
            SoundManager.Instance.SetWindVolume(0f);
            SoundManager.Instance.PlayVictory();
            speedPostProcessEffect.SetFloat("_Speed", 0);
            SceneManager.LoadScene("Menu");
        }
    }
}
