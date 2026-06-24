using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QAPanelController : MonoBehaviour
{
    public static QAPanelController Instance;

    [Header("UI Binding (leave empty to auto-create)")]
    public GameObject panelObj;
    public TMP_Text titleText;
    public TMP_Text operateText;
    public TMP_Text faultText;

    [Header("Auto-created UI Settings")]
    public float panelWidth = 420f;
    public float panelHeight = 360f;
    public Vector2 panelPosition = new Vector2(24f, -140f);

    private RectTransform panelRect;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (panelObj == null)
        {
            CreatePanelUI();
        }
        else
        {
            panelObj.SetActive(false);
        }
    }

    void CreatePanelUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Panel root
        panelObj = new GameObject("QAPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObj.transform.SetParent(canvas.transform, false);
        panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = panelPosition;
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        Image bg = panelObj.GetComponent<Image>();
        bg.color = new Color(0.04f, 0.07f, 0.14f, 0.96f);

        // "X" close button (top-right corner)
        GameObject closeBtn = new GameObject("CloseBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtn.transform.SetParent(panelObj.transform, false);
        RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-6f, -6f);
        closeRect.sizeDelta = new Vector2(28f, 28f);
        closeBtn.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.22f, 1f);
        closeBtn.GetComponent<Button>().onClick.AddListener(HideQA);

        GameObject closeX = new GameObject("XText", typeof(RectTransform), typeof(CanvasRenderer));
        closeX.transform.SetParent(closeBtn.transform, false);
        TMP_Text xTxt = closeX.AddComponent<TextMeshProUGUI>();
        xTxt.text = "\u2715";
        xTxt.fontSize = 14f;
        xTxt.color = new Color(0.55f, 0.58f, 0.65f, 1f);
        xTxt.alignment = TextAlignmentOptions.Center;
        RectTransform xRect = closeX.GetComponent<RectTransform>();
        xRect.anchorMin = Vector2.zero;
        xRect.anchorMax = Vector2.one;
        xRect.sizeDelta = Vector2.zero;

        // Title
        titleText = CreateText("TitleText", panelObj.transform, 18f, TextAlignmentOptions.MidlineLeft,
            new Vector2(12f, -10f), new Vector2(-40f, -34f), new Color(0.65f, 0.72f, 0.85f, 1f), FontStyles.Bold);

        // Separator line
        GameObject sep = new GameObject("Separator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sep.transform.SetParent(panelObj.transform, false);
        RectTransform sepRect = sep.GetComponent<RectTransform>();
        sepRect.anchorMin = new Vector2(0f, 1f);
        sepRect.anchorMax = new Vector2(1f, 1f);
        sepRect.pivot = new Vector2(0.5f, 1f);
        sepRect.anchoredPosition = new Vector2(0f, -38f);
        sepRect.sizeDelta = new Vector2(-20f, 1f);
        sep.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.28f, 1f);

        // Section headers + content
        CreateSectionLabel("\u6807\u51c6\u64cd\u4f5c", panelObj.transform, -46f);
        operateText = CreateText("OperateText", panelObj.transform, 13f, TextAlignmentOptions.TopLeft,
            new Vector2(12f, -66f), new Vector2(-14f, -180f), new Color(0.75f, 0.78f, 0.85f, 1f), FontStyles.Normal);

        CreateSectionLabel("\u6545\u969c\u5904\u7406", panelObj.transform, -195f);
        faultText = CreateText("FaultText", panelObj.transform, 13f, TextAlignmentOptions.TopLeft,
            new Vector2(12f, -215f), new Vector2(-14f, -panelHeight + 34f), new Color(0.85f, 0.65f, 0.4f, 1f), FontStyles.Normal);

        panelObj.SetActive(false);
    }

    TMP_Text CreateText(string name, Transform parent, float fontSize, TextAlignmentOptions alignment,
        Vector2 pos, Vector2 offsetMax, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.enableWordWrapping = true;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.offsetMin = new Vector2(0, offsetMax.y);
        rect.offsetMax = offsetMax;
        return tmp;
    }

    void CreateSectionLabel(string label, Transform parent, float yPos)
    {
        GameObject go = new GameObject("SectionLabel", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "\u25a0 " + label;
        tmp.fontSize = 12f;
        tmp.color = new Color(0.45f, 0.52f, 0.65f, 1f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(12f, yPos);
        rect.sizeDelta = new Vector2(-24f, 18f);
    }

    public void ShowQA(string btnName, string operateInfo, string faultInfo)
    {
        if (panelObj == null) return;
        if (titleText != null) titleText.text = btnName;
        if (operateText != null) operateText.text = operateInfo;
        if (faultText != null) faultText.text = faultInfo;
        panelObj.SetActive(true);
    }

    public void HideQA()
    {
        if (panelObj != null) panelObj.SetActive(false);
    }
}
