using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ARInteractor : MonoBehaviour
{
    public Camera arCamera;
    public LayerMask interactableMask;
    public float maxDistance = 20f;

    [Header("Hit Detection Tuning")]
    [Tooltip("SphereCast radius - larger = easier to tap, smaller = more precise")]
    public float hitRadius = 0.03f;
    [Tooltip("Number of ray samples around the touch point")]
    public int raySamples = 1;
    [Tooltip("Spread radius for multi-sampling in screen pixels")]
    public float sampleSpread = 6f;

    void Update()
    {
        if (!TryGetPointerDown(out Vector2 pos)) return;
        if (arCamera == null) arCamera = Camera.main;
        if (arCamera == null) return;

        int mask = interactableMask.value == 0 ? Physics.DefaultRaycastLayers : interactableMask.value;

        RaycastHit? bestHit = TryMultiSampleHit(pos, mask);

        if (bestHit.HasValue)
        {
            RaycastHit hit = bestHit.Value;

            var btn = hit.collider.GetComponentInParent<ClickableButton3D>();
            if (btn != null && btn.targetText != null)
            {
                btn.targetText.Toggle();
            }

            var toggle = hit.collider.GetComponentInParent<ToggleAnimOnTap>();
            if (toggle != null)
            {
                toggle.Toggle();
                return;
            }
        }
    }

    RaycastHit? TryMultiSampleHit(Vector2 screenPos, int mask)
    {
        float halfSample = sampleSpread * 0.5f;
        float closest = float.MaxValue;
        RaycastHit? bestHit = null;

        for (int i = 0; i < raySamples; i++)
        {
            Vector2 offset = Vector2.zero;
            if (i > 0)
            {
                float angle = (i - 1) * (Mathf.PI * 2f) / (raySamples - 1);
                offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * halfSample;
            }

            Vector2 samplePos = screenPos + offset;
            Ray ray = arCamera.ScreenPointToRay(samplePos);

            if (Physics.SphereCast(ray, hitRadius, out RaycastHit hit, maxDistance, mask, QueryTriggerInteraction.Collide))
            {
                if (hit.distance < closest)
                {
                    closest = hit.distance;
                    bestHit = hit;
                }
            }
        }

        return bestHit;
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
