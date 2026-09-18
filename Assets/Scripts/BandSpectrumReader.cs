using UnityEngine;
using EmotivUnityPlugin;

public class BandSpectrumReader : MonoBehaviour
{
    [Header("UI")]
    public BandSpectrumGraphUI graphUI;
    public VerticalBandScaleUI scaleUI;

    [Header("Settings")]
    public float updateInterval = 0.1f;
    public float smoothing      = 0.2f;

    private float timer = 0f;
    private float sTheta, sAlpha, sLowBeta, sHighBeta, sGamma;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;

        // Read from AF3+AF4 (main) and F3+F4 (cognitive)
        float theta = Avg(
            DataStreamManager.Instance.GetThetaData(Channel_t.CHAN_AF3),
            DataStreamManager.Instance.GetThetaData(Channel_t.CHAN_AF4),
            DataStreamManager.Instance.GetThetaData(Channel_t.CHAN_F3),
            DataStreamManager.Instance.GetThetaData(Channel_t.CHAN_F4)
        );
        float alpha = Avg(
            DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_AF3),
            DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_AF4),
            DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_F3),
            DataStreamManager.Instance.GetAlphaData(Channel_t.CHAN_F4)
        );
        float lowBeta = Avg(
            DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_AF3),
            DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_AF4),
            DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_F3),
            DataStreamManager.Instance.GetLowBetaData(Channel_t.CHAN_F4)
        );
        float highBeta = Avg(
            DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_AF3),
            DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_AF4),
            DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_F3),
            DataStreamManager.Instance.GetHighBetaData(Channel_t.CHAN_F4)
        );
        float gamma = Avg(
            DataStreamManager.Instance.GetGammaData(Channel_t.CHAN_AF3),
            DataStreamManager.Instance.GetGammaData(Channel_t.CHAN_AF4),
            DataStreamManager.Instance.GetGammaData(Channel_t.CHAN_F3),
            DataStreamManager.Instance.GetGammaData(Channel_t.CHAN_F4)
        );

        // Smooth
        sTheta    = Mathf.Lerp(sTheta,    theta,    smoothing);
        sAlpha    = Mathf.Lerp(sAlpha,    alpha,    smoothing);
        sLowBeta  = Mathf.Lerp(sLowBeta,  lowBeta,  smoothing);
        sHighBeta = Mathf.Lerp(sHighBeta, highBeta, smoothing);
        sGamma    = Mathf.Lerp(sGamma,    gamma,    smoothing);

        // Update graph
        graphUI.UpdateBands(sTheta, sAlpha, sLowBeta, sHighBeta, sGamma);

        // Update left bar — focus level (beta/alpha ratio)
        float focus = (sLowBeta + sHighBeta) / (sAlpha + 0.001f);
        scaleUI.UpdateScale(Mathf.Clamp01(focus / 3f));
    }

    private float Avg(params double[] vals)
    {
        float sum = 0f;
        int n = 0;
        foreach (double v in vals)
            if (v > 0) { sum += (float)v; n++; }
        return n > 0 ? sum / n : 0f;
    }
}