using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ARInteractor : MonoBehaviour
{
    public Camera arCamera;
    public LayerMask interactableMask;
    public float maxDistance = 20f;

    void Update()
    {
        if (!TryGetPointerDown(out Vector2 pos)) return;
        if (arCamera == null) arCamera = Camera.main;
        if (arCamera == null) return;

        Ray ray = arCamera.ScreenPointToRay(pos);
        int mask = interactableMask.value == 0 ? Physics.DefaultRaycastLayers : interactableMask.value;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, mask, QueryTriggerInteraction.Collide))
        {
            var btn = hit.collider.GetComponentInParent<ClickableButton3D>();
            if (btn != null && btn.targetText != null)
            {
                btn.targetText.Toggle();
            }

            var toggle = hit.collider.GetComponentInParent<ToggleAnimOnTap>();
            if (toggle != null)
                {
                    toggle.Toggle();
                    return; // 如果你不想让其它交互同时触发
                }
        }
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
