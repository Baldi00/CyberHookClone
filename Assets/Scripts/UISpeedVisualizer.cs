using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISpeedVisualizer : MonoBehaviour
{
    [SerializeField]
    private PlayerMover playerMover;
    [SerializeField]
    private Slider slider;
    [SerializeField]
    private Image fillImage;
    [SerializeField]
    private Color slowColor;
    [SerializeField]
    private Color fastColor;
    [SerializeField]
    TextMeshProUGUI speedText;

    void Update()
    {
        float speed = playerMover.GetCurrentSpeed();
        float percentage = playerMover.GetCurrentSpeedPercentage();
        speed = Mathf.Lerp(0, 30, Mathf.InverseLerp(7.5f, 30, speed));
        float interpolator = percentage * percentage * percentage * percentage * percentage;
        slider.value = percentage + Mathf.Lerp(0, Mathf.Sin(60 * Time.time) * 0.02f, interpolator);
        fillImage.color = Color.Lerp(slowColor, fastColor, percentage);
        speedText.text = $"{(int)speed} Km/h";
    }
}
