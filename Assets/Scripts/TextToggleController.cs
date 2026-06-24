using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TextToggleController : MonoBehaviour
{
    [Header("Show/Hide Target (usually this GameObject)")]
    public GameObject textRoot;
    [Header("Optional: Text to show when opened")]
    public TMP_Text tmpToFill;
    [TextArea] public string textWhenOpen;

    public bool startHidden = true;
    public bool faceCameraWhenShown = true;
    TMP_Text[] cachedTexts;
    Quaternion initialLocalRotation;

    // Tech-style accent bar
    private GameObject accentBar;

    void Awake()
    {
        if (textRoot == null) textRoot = gameObject;
        initialLocalRotation = textRoot.transform.localRotation;
        if (startHidden) textRoot.SetActive(false);
        if (tmpToFill == null) tmpToFill = GetComponentInChildren<TMP_Text>(true);
        CacheTexts();
        SetupBackground();
        CreateAccentBar();
    }

    void SetupBackground()
    {
        if (textRoot == null) return;

        Image bg = textRoot.GetComponent<Image>();
        if (bg == null)
        {
            bg = textRoot.AddComponent<Image>();
        }
        // Dark glass-morphism panel
        bg.color = new Color(0.04f, 0.06f, 0.12f, 0.94f);

        // Add subtle outline via Outline component or fallback Shadow
        Outline outline = textRoot.GetComponent<Outline>();
        if (outline == null)
        {
            outline = textRoot.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0.18f, 0.55f, 0.92f, 0.25f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        RectTransform rect = textRoot.GetComponent<RectTransform>();
        if (rect != null)
        {
            if (rect.sizeDelta.x < 10f && rect.sizeDelta.y < 10f)
            {
                rect.sizeDelta = new Vector2(620f, 220f);
            }
        }
    }

    void CreateAccentBar()
    {
        if (textRoot == null) return;

        Transform existing = textRoot.transform.Find("_TechAccentBar");
        if (existing != null) return;

        accentBar = new GameObject("_TechAccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accentBar.transform.SetParent(textRoot.transform, false);

        RectTransform barRect = accentBar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0.15f);
        barRect.anchorMax = new Vector2(0f, 0.85f);
        barRect.pivot = new Vector2(0f, 0.5f);
        barRect.sizeDelta = new Vector2(4f, 0f);
        barRect.anchoredPosition = new Vector2(0f, 0f);

        Image barImg = accentBar.GetComponent<Image>();
        barImg.color = new Color(0.18f, 0.55f, 0.92f, 0.85f);
        barImg.raycastTarget = false;

        // Bottom glow dot
        GameObject glow = new GameObject("_GlowDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glow.transform.SetParent(accentBar.transform, false);
        RectTransform glowRect = glow.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0f);
        glowRect.anchorMax = new Vector2(0.5f, 0f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(10f, 10f);
        glowRect.anchoredPosition = new Vector2(0f, -8f);
        glow.GetComponent<Image>().color = new Color(0.18f, 0.55f, 0.92f, 0.6f);
        glow.GetComponent<Image>().raycastTarget = false;
    }

    void SetupTextStyles()
    {
        if (cachedTexts == null) CacheTexts();
        foreach (var text in cachedTexts)
        {
            if (text == null) continue;
            text.color = new Color(0.92f, 0.94f, 0.97f, 1f);
            if (text.font != null)
            {
                text.fontStyle = FontStyles.Normal;
            }
        }
    }

    public void Show()
    {
        if (textRoot == null) textRoot = gameObject;
        textRoot.SetActive(true);
        FixMirroredPanelIfNeeded();
        EnableTexts();
        // Force ALL text to bright white — scene-saved colors override Awake
        if (cachedTexts == null || cachedTexts.Length == 0) CacheTexts();
        foreach (var text in cachedTexts)
        {
            if (text == null) continue;
            text.color = new Color(0.05f, 0.06f, 0.10f, 1f);
            text.fontStyle = FontStyles.Normal;
        }
        if (tmpToFill != null && !string.IsNullOrEmpty(textWhenOpen))
        {
            tmpToFill.text = textWhenOpen;
            tmpToFill.fontStyle = FontStyles.Bold;
            tmpToFill.color = new Color(0.02f, 0.03f, 0.08f, 1f);
        }
    }

    public void Hide() => textRoot.SetActive(false);

    public void Toggle()
    {
        if (textRoot.activeSelf)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    void CacheTexts()
    {
        var root = textRoot != null ? textRoot : gameObject;
        cachedTexts = root.GetComponentsInChildren<TMP_Text>(true);
    }

    void EnableTexts()
    {
        if (cachedTexts == null || cachedTexts.Length == 0) CacheTexts();

        foreach (var text in cachedTexts)
        {
            if (text == null) continue;
            text.enabled = true;
        }
    }

    void FixMirroredPanelIfNeeded()
    {
        if (!faceCameraWhenShown || textRoot == null) return;

        Camera camera = Camera.main;
        if (camera == null) return;

        Transform rootTransform = textRoot.transform;
        Quaternion normal = initialLocalRotation;
        Quaternion flipped = initialLocalRotation * Quaternion.Euler(0f, 180f, 0f);

        rootTransform.localRotation = normal;
        float normalScore = GetReadableFacingScore(rootTransform, camera);

        rootTransform.localRotation = flipped;
        float flippedScore = GetReadableFacingScore(rootTransform, camera);

        rootTransform.localRotation = flippedScore > normalScore ? flipped : normal;
    }

    float GetReadableFacingScore(Transform rootTransform, Camera camera)
    {
        Vector3 toCamera = camera.transform.position - rootTransform.position;
        float score = Vector3.Dot(rootTransform.forward, toCamera.normalized);

        Vector3 center = camera.WorldToScreenPoint(rootTransform.position);
        Vector3 right = camera.WorldToScreenPoint(rootTransform.position + rootTransform.right * 0.1f);
        if (center.z > 0f && right.z > 0f && right.x > center.x)
        {
            score += 2f;
        }

        return score;
    }
}