using UnityEngine;
using easyar;
using UIImage = UnityEngine.UI.Image;

public class ARImageTracker : MonoBehaviour
{
    public ImageTargetController imageTargetController;
    public GameObject hintUI;
    public bool useTransparentHintBorder = true;
    public Color hintBorderColor = new Color(0.02f, 0.85f, 0.08f, 0.85f);
    public float hintBorderThickness = 6f;
    public Vector2 hintBorderSize = new Vector2(280f, 300f);

    private void Start()
    {
        if (imageTargetController == null)
        {
            Debug.LogError("Please assign ImageTargetController in Inspector.");
            return;
        }

        ConfigureHintBorder();

        imageTargetController.TargetFound += () =>
        {
            OnTargetFound(imageTargetController, imageTargetController.Target);
        };

        imageTargetController.TargetLost += () =>
        {
            OnTargetLost(imageTargetController, imageTargetController.Target);
        };
    }

    private void OnTargetFound(ImageTargetController sender, Target target)
    {
        if (hintUI != null)
        {
            hintUI.SetActive(false);
        }

        Debug.Log("Image target found.");
    }

    private void OnTargetLost(ImageTargetController sender, Target target)
    {
        if (hintUI != null)
        {
            hintUI.SetActive(true);
        }

        Debug.Log("Image target lost.");
    }

    private void ConfigureHintBorder()
    {
        if (!useTransparentHintBorder || hintUI == null)
        {
            return;
        }

        UIImage fillImage = hintUI.GetComponent<UIImage>();
        if (fillImage == null)
        {
            fillImage = hintUI.GetComponentInChildren<UIImage>(true);
        }

        if (fillImage == null)
        {
            return;
        }

        Color transparentFill = fillImage.color;
        transparentFill.a = 0f;
        fillImage.color = transparentFill;
        fillImage.raycastTarget = false;

        RectTransform parentRect = fillImage.rectTransform;
        if (hintBorderSize.x > 0f && hintBorderSize.y > 0f)
        {
            parentRect.sizeDelta = hintBorderSize;
        }

        CreateOrUpdateBorderLine(parentRect, "TransparentHintBorder_Top",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, hintBorderThickness), Vector2.zero);
        CreateOrUpdateBorderLine(parentRect, "TransparentHintBorder_Bottom",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, hintBorderThickness), Vector2.zero);
        CreateOrUpdateBorderLine(parentRect, "TransparentHintBorder_Left",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(hintBorderThickness, 0f), Vector2.zero);
        CreateOrUpdateBorderLine(parentRect, "TransparentHintBorder_Right",
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            new Vector2(hintBorderThickness, 0f), Vector2.zero);
    }

    private void CreateOrUpdateBorderLine(
        RectTransform parent,
        string lineName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition)
    {
        Transform existing = parent.Find(lineName);
        GameObject lineObject = existing != null
            ? existing.gameObject
            : new GameObject(lineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(UIImage));

        lineObject.transform.SetParent(parent, false);
        lineObject.transform.SetAsLastSibling();

        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        UIImage image = lineObject.GetComponent<UIImage>();
        image.color = hintBorderColor;
        image.raycastTarget = false;
    }
}
