using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BulletTimeManager : MonoBehaviour
{
    [SerializeField]
    private float postProcessingAnimationDuration;
    [SerializeField]
    private VolumeProfile postProcessingProfile;
    [SerializeField]
    private PlayerMover playerMover;

    private ChromaticAberration chromaticAberration;
    private Vignette vignette;

    void Awake()
    {
        postProcessingProfile.TryGet<ChromaticAberration>(out chromaticAberration);
        postProcessingProfile.TryGet<Vignette>(out vignette);
    }

    void Update()
    {
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame)
        {
            playerMover.SetBulletTime(true);
            StopAllCoroutines();
            StartCoroutine(StartPostProcessAnimation());
        }
        if (Keyboard.current.leftShiftKey.wasReleasedThisFrame)
        {
            playerMover.SetBulletTime(false);
            StopAllCoroutines();
            StartCoroutine(StopPostProcessAnimation());
        }

        if (Keyboard.current.leftShiftKey.isPressed)
            Time.timeScale = 0.3f;
        else
            Time.timeScale = 1f;
    }

    private IEnumerator StartPostProcessAnimation()
    {

        chromaticAberration.active = true;
        vignette.active = true;

        float timer = 0;
        while (timer < postProcessingAnimationDuration)
        {
            chromaticAberration.intensity.Override(Mathf.Lerp(0, 1, timer / postProcessingAnimationDuration));
            vignette.intensity.Override(Mathf.Lerp(0, 0.5f, timer / postProcessingAnimationDuration));
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator StopPostProcessAnimation()
    {
        float timer = 0;
        while (timer < postProcessingAnimationDuration)
        {
            chromaticAberration.intensity.Override(Mathf.Lerp(1, 0, timer / postProcessingAnimationDuration));
            vignette.intensity.Override(Mathf.Lerp(0.5f, 0, timer / postProcessingAnimationDuration));
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        chromaticAberration.active = false;
        vignette.active = false;
    }
}
