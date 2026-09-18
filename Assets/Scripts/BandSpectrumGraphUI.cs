using UnityEngine;
using System.Collections.Generic;

public class BandSpectrumGraphUI : MonoBehaviour
{
    [Header("Line Renderers — bands")]
    public LineRenderer lineTheta;
    public LineRenderer lineAlpha;
    public LineRenderer lineLowBeta;
    public LineRenderer lineHighBeta;
    public LineRenderer lineGamma;

    [Header("Axes")]
    public LineRenderer axisX;
    public LineRenderer axisY;

    [Header("Graph Bounds")]
    public float graphLeft   = -6.5f;
    public float graphRight  =  6.5f;
    public float graphBottom = -1.5f;
    public float graphTop    =  2.5f;

    [Header("History")]
    public int historyLength = 100;

    [Header("Value Range")]
    public float maxValue = 5f;

    private Queue<float> histTheta    = new Queue<float>();
    private Queue<float> histAlpha    = new Queue<float>();
    private Queue<float> histLowBeta  = new Queue<float>();
    private Queue<float> histHighBeta = new Queue<float>();
    private Queue<float> histGamma    = new Queue<float>();

    void Start()
    {
        // Setup all line renderers
        SetupLine(lineTheta,    new Color(0.31f, 0.76f, 0.97f), 0.05f);
        SetupLine(lineAlpha,    new Color(0.40f, 0.73f, 0.42f), 0.05f);
        SetupLine(lineLowBeta,  new Color(1f,    0.93f, 0.35f), 0.05f);
        SetupLine(lineHighBeta, new Color(1f,    0.65f, 0.15f), 0.05f);
        SetupLine(lineGamma,    new Color(0.94f, 0.33f, 0.31f), 0.05f);
        SetupLine(axisX,        Color.white, 0.04f);
        SetupLine(axisY,        Color.white, 0.04f);

        // Pre-fill history
        for (int i = 0; i < historyLength; i++)
        {
            histTheta.Enqueue(0f);
            histAlpha.Enqueue(0f);
            histLowBeta.Enqueue(0f);
            histHighBeta.Enqueue(0f);
            histGamma.Enqueue(0f);
        }

        DrawAxes();
        RedrawAll();
    }

    public void UpdateBands(float theta, float alpha, float lowBeta,
                            float highBeta, float gamma)
    {
        Push(histTheta,    theta);
        Push(histAlpha,    alpha);
        Push(histLowBeta,  lowBeta);
        Push(histHighBeta, highBeta);
        Push(histGamma,    gamma);

        RedrawAll();
    }

    private void DrawAxes()
    {
        // X axis — horizontal line at bottom
        if (axisX != null)
        {
            axisX.positionCount = 2;
            axisX.SetPosition(0, new Vector3(graphLeft,  graphBottom, -0.5f));
            axisX.SetPosition(1, new Vector3(graphRight, graphBottom, -0.5f));
        }

        // Y axis — vertical line at left
        if (axisY != null)
        {
            axisY.positionCount = 2;
            axisY.SetPosition(0, new Vector3(graphLeft, graphBottom, -0.5f));
            axisY.SetPosition(1, new Vector3(graphLeft, graphTop,    -0.5f));
        }
    }

    private void RedrawAll()
    {
        DrawBandLine(lineTheta,    histTheta);
        DrawBandLine(lineAlpha,    histAlpha);
        DrawBandLine(lineLowBeta,  histLowBeta);
        DrawBandLine(lineHighBeta, histHighBeta);
        DrawBandLine(lineGamma,    histGamma);
    }

    private void DrawBandLine(LineRenderer lr, Queue<float> history)
    {
        if (lr == null) return;

        float[] values = history.ToArray();
        int n = values.Length;
        lr.positionCount = n;

        for (int i = 0; i < n; i++)
        {
            float t    = (float)i / (n - 1);
            float x    = Mathf.Lerp(graphLeft, graphRight, t);
            float norm = Mathf.Clamp01(values[i] / maxValue);
            float y    = Mathf.Lerp(graphBottom, graphTop, norm);
            lr.SetPosition(i, new Vector3(x, y, -1f));
        }
    }

    private void Push(Queue<float> q, float value)
    {
        q.Enqueue(value);
        if (q.Count > historyLength) q.Dequeue();
    }

    private void SetupLine(LineRenderer lr, Color color, float width)
    {
        if (lr == null) return;
        lr.useWorldSpace = true;
        lr.startWidth    = width;
        lr.endWidth      = width;
        lr.startColor    = color;
        lr.endColor      = color;
    }
}