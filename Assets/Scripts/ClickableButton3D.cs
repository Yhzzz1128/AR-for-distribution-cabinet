using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ClickableButton3D : MonoBehaviour
{
    [Header("Corresponding Text Panel (TextToggleController)")]
    public TextToggleController targetText;

    [Header("Button Visual Settings")]
    public Color highlightColor = new Color(0.1f, 0.85f, 0.2f, 0.95f);
    public Color defaultColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
    public float pulseSpeed = 2f;
    public float pulseMin = 0.5f;
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
