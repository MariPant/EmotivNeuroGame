using UnityEngine;
using UnityEngine.UI;

public class VerticalBandScaleUI : MonoBehaviour
{
    [SerializeField] private Image[] segments;

    [SerializeField] private Color inactiveColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);

    [Header("Band colors")]
    [SerializeField] private Color thetaColor = new Color(0.31f,0.76f,0.97f);
    [SerializeField] private Color alphaColor = new Color(0.40f,0.73f,0.42f);
    [SerializeField] private Color lowBetaColor = new Color(1f,0.93f,0.35f);
    [SerializeField] private Color highBetaColor = new Color(1f,0.65f,0.15f);
    [SerializeField] private Color gammaColor = new Color(0.94f,0.33f,0.31f);

    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 1f;

    public void UpdateScale(float value)
    {
        float normalized = Mathf.InverseLerp(minValue,maxValue,value);

        int activeSegments = Mathf.Clamp(
            Mathf.FloorToInt(normalized * segments.Length),
            0,
            segments.Length
        );

        for(int i=0;i<segments.Length;i++)
        {
            if(i < activeSegments)
                segments[i].color = GetBandColor(i);
            else
                segments[i].color = inactiveColor;
        }
    }

    Color GetBandColor(int index)
    {
        if(index <= 1) return thetaColor;
        if(index <= 3) return alphaColor;
        if(index <= 5) return lowBetaColor;
        if(index <= 8) return highBetaColor;
        return gammaColor;
    }
}