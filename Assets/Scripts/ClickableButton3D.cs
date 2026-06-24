using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ClickableButton3D : MonoBehaviour
{
    [Header("Corresponding Text Panel (TextToggleController)")]
    public TextToggleController targetText;

    [Header("QA Content (for QAPanelController)")]
    public string buttonName;
    [TextArea(3, 10)]
    public string standardOperation;
    [TextArea(3, 10)]
    public string faultHandling;

    [Header("Button Visual Settings")]
    public Color highlightColor = new Color(0.18f, 0.55f, 0.92f, 0.92f);
    public Color defaultColor = new Color(0.45f, 0.48f, 0.55f, 0.55f);
    public float pulseSpeed = 1.4f;
    public float pulseMin = 0.72f;
    public float pulseMax = 1f;

    private Renderer[] childRenderers;
    private MaterialPropertyBlock propBlock;
    private Color currentColor;
    private float pulseTime;

    void Awake()
    {
        childRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        SetAllColors(highlightColor);
    }

    // Called by ARInteractor when this button is tapped
    public void OnTapped()
    {
        if (!string.IsNullOrWhiteSpace(buttonName))
        {
            QAPanelController.Instance?.ShowQA(buttonName, standardOperation, faultHandling);
        }
    }

    void Update()
    {
        // Pulsing highlight effect
        pulseTime += Time.deltaTime * pulseSpeed;
        float t = (Mathf.Sin(pulseTime) + 1f) * 0.5f;
        float brightness = Mathf.Lerp(pulseMin, pulseMax, t);
        currentColor = new Color(highlightColor.r * brightness, highlightColor.g * brightness, highlightColor.b * brightness, highlightColor.a);
        SetAllColors(currentColor);
    }

    void SetAllColors(Color c)
    {
        foreach (var r in childRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", c);
            r.SetPropertyBlock(propBlock);
        }
    }
}