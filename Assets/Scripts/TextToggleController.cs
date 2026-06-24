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

    void Awake()
    {
        if (textRoot == null) textRoot = gameObject;
        initialLocalRotation = textRoot.transform.localRotation;
        if (startHidden) textRoot.SetActive(false);
        if (tmpToFill == null) tmpToFill = GetComponentInChildren<TMP_Text>(true);
        CacheTexts();
        SetupBackground();
    }

    void SetupBackground()
    {
        if (textRoot == null) return;

        Image bg = textRoot.GetComponent<Image>();
        if (bg == null)
        {
            bg = textRoot.AddComponent<Image>();
        }
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);

        RectTransform rect = textRoot.GetComponent<RectTransform>();
        if (rect != null)
        {
            if (rect.sizeDelta.x < 10f && rect.sizeDelta.y < 10f)
            {
                rect.sizeDelta = new Vector2(600f, 200f);
            }
        }
    }

    public void Show()
    {
        if (textRoot == null) textRoot = gameObject;
        textRoot.SetActive(true);
        FixMirroredPanelIfNeeded();
        EnableTexts();
        if (tmpToFill != null && !string.IsNullOrEmpty(textWhenOpen))
            tmpToFill.text = textWhenOpen;
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
