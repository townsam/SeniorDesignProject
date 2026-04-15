using UnityEngine;

public class FloatUI : MonoBehaviour
{
    public float floatAmplitude = 10f;   // How high/low it moves (in UI units)
    public float floatSpeed = 2f;        // How fast it moves
    public float offset = 0f;            // Phase offset so letters aren't synced

    private RectTransform rectTransform;
    private Vector2 startPos;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed + offset) * floatAmplitude;
        rectTransform.anchoredPosition = new Vector2(startPos.x, newY);
    }
}