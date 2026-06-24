using UnityEngine;
using System.Collections.Generic;

public class ARInteractor : MonoBehaviour
{
    public Camera arCamera;
    public LayerMask interactableMask;
    public float maxDistance = 20f;

    [Header("Color Match Tolerance")]
    [Tooltip("Max color difference (0-1) allowed for a match. Lower = stricter.")]
    public float colorTolerance = 0.15f;

    private ClickableButton3D[] allButtons;

    void Start()
    {
        allButtons = FindObjectsOfType<ClickableButton3D>();
    }

    void Update()
    {
        if (!TryGetPointerDown(out Vector2 pos)) return;
        if (arCamera == null) arCamera = Camera.main;
        if (arCamera == null) return;

        int mask = interactableMask.value == 0 ? Physics.DefaultRaycastLayers : interactableMask.value;
        Ray ray = arCamera.ScreenPointToRay(pos);

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, mask, QueryTriggerInteraction.Collide))
            return;

        // Refresh button registry if needed
        if (allButtons == null || allButtons.Length == 0)
        {
            allButtons = FindObjectsOfType<ClickableButton3D>();
        }

        // Try to match by object reference first (exact hit on a button collider)
        var btn = hit.collider.GetComponentInParent<ClickableButton3D>();
        if (btn != null)
        {
            if (btn.targetText != null)
                btn.targetText.Toggle();
            return;
        }

        // Fallback: match by material color at the hit point
        Renderer hitRenderer = hit.collider.GetComponentInParent<Renderer>();
        if (hitRenderer != null)
        {
            Color hitColor = GetHitColor(hitRenderer, hit);
            ClickableButton3D matched = MatchByColor(hitColor);
            if (matched != null && matched.targetText != null)
            {
                matched.targetText.Toggle();
                return;
            }
        }

        // Toggle animation support
        var toggle = hit.collider.GetComponentInParent<ToggleAnimOnTap>();
        if (toggle != null)
        {
            toggle.Toggle();
        }
    }

    Color GetHitColor(Renderer r, RaycastHit hit)
    {
        // Try to read the actual pixel color from the texture at the hit UV
        Material mat = r.sharedMaterial;
        if (mat != null && mat.HasProperty("_MainTex") && mat.mainTexture is Texture2D tex)
        {
            if (hit.textureCoord != Vector2.zero || hit.textureCoord2 != Vector2.zero)
            {
                Vector2 uv = hit.textureCoord;
                int x = Mathf.FloorToInt(uv.x * tex.width);
                int y = Mathf.FloorToInt(uv.y * tex.height);
                x = Mathf.Clamp(x, 0, tex.width - 1);
                y = Mathf.Clamp(y, 0, tex.height - 1);
                return tex.GetPixel(x, y);
            }
        }

        // Fallback: use material''s _Color property
        if (mat != null && mat.HasProperty("_Color"))
        {
            return mat.GetColor("_Color");
        }

        // Last resort: use property block
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        return block.GetColor("_Color");
    }

    ClickableButton3D MatchByColor(Color hitColor)
    {
        if (allButtons == null) return null;

        ClickableButton3D best = null;
        float bestDist = float.MaxValue;

        foreach (var b in allButtons)
        {
            if (b == null) continue;
            float dist = ColorDistance(hitColor, b.highlightColor);
            if (dist < bestDist && dist <= colorTolerance)
            {
                bestDist = dist;
                best = b;
            }
        }

        return best;
    }

    float ColorDistance(Color a, Color b)
    {
        // Weighted perceptual distance: hue matters more
        float hueA, satA, valA, hueB, satB, valB;
        Color.RGBToHSV(a, out hueA, out satA, out valA);
        Color.RGBToHSV(b, out hueB, out satB, out valB);

        float hueDist = Mathf.Abs(hueA - hueB);
        if (hueDist > 0.5f) hueDist = 1f - hueDist;

        float satDist = Mathf.Abs(satA - satB);
        float valDist = Mathf.Abs(valA - valB);

        return hueDist * 2f + satDist * 1f + valDist * 0.5f;
    }

    bool TryGetPointerDown(out Vector2 pos)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        { pos = Input.GetTouch(0).position; return true; }

        if (Input.GetMouseButtonDown(0))
        { pos = Input.mousePosition; return true; }

        pos = default; return false;
    }
}
