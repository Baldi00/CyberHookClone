using TMPro;
using UnityEngine;

public class UITimer : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI timerUi;

    private float timer = 0;

    void Update()
    {
        if (Time.timeScale == 0)
            return;
        timer += Time.unscaledDeltaTime;
        timerUi.text = $"{GetMinutes():00}:{GetSeconds():00}:{GetCents():00}";
    }

    private int GetMinutes()
    {
        return (int)(timer / 60);
    }

    private int GetSeconds()
    {
        return (int)(timer - 60 * GetMinutes());
    }

    private int GetCents()
    {
        return (int)(timer % 1 * 100);
    }
}
