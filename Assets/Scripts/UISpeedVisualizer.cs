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
        slider.value = percentage;
        fillImage.color = Color.Lerp(slowColor, fastColor, percentage);
        speedText.text = $"{(int)speed} Km/h";
    }
}
