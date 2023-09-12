using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public AudioSource source;
    public AudioClip footstep;
    public AudioClip shoot;
    public AudioClip hook;
    public AudioClip bulletTime;
    public AudioClip jump;
    public AudioClip victory;
    public AudioSource wind;

    private static SoundManager _instance = null;

    public static SoundManager Instance { get => _instance; }

    void Awake()
    {
        if (_instance == null)
            _instance = this;
        else if (_instance != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public void PlayFootstep() => source.PlayOneShot(footstep);
	public void PlayShoot() => source.PlayOneShot(shoot);
	public void PlayHook() => source.PlayOneShot(hook);
	public void PlayBulletTime() => source.PlayOneShot(bulletTime);
	public void PlayJump() => source.PlayOneShot(jump);
	public void PlayVictory() => source.PlayOneShot(victory);

    public void SetWindVolume(float percentage)
    {
        wind.volume = percentage;
    }

}