using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class QAPageController : MonoBehaviour
{
    public TMP_FontAsset menuFont;
    public Canvas canvas;
    public System.Action OnBackToMenu;

    private GameObject pagePanel;
    private TMP_InputField searchInput;
    private GameObject resultContent;
    private ScrollRect scrollRect;
    private RectTransform contentRect;
    private List<GameObject> resultItems = new List<GameObject>();

    public void Show()
    {
        // Auto-load Chinese font
        if (menuFont == null)
        {
            var mgr = FindObjectOfType<AI_Search_Manager>();
            if (mgr != null && mgr.chineseFont != null)
                menuFont = mgr.chineseFont;
        }
        if (pagePanel != null) { pagePanel.SetActive(true); return; }
        CreatePage();
    }

    public void Hide()
    {
        if (pagePanel != null) pagePanel.SetActive(false);
    }

    void CreatePage()
    {
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        float s = Mathf.Clamp(Screen.width / 540f, 1f, 2f);

        // Full-screen background
        pagePanel = new GameObject("QAPage", typeof(RectTransform), typeof(Image));
        pagePanel.transform.SetParent(canvas.transform, false);
        pagePanel.transform.SetAsLastSibling();
        RectTransform pr = pagePanel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.offsetMin = Vector2.zero; pr.offsetMax = Vector2.zero;
        pagePanel.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.12f, 1f);

        // Top bar
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(pagePanel.transform, false);
        RectTransform tb = topBar.GetComponent<RectTransform>();
        tb.anchorMin = new Vector2(0, 1); tb.anchorMax = new Vector2(1, 1);
        tb.pivot = new Vector2(0.5f, 1);
        tb.sizeDelta = new Vector2(0, 56f * s); tb.anchoredPosition = Vector2.zero;
        topBar.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.18f, 0.95f);

        // Back button
        GameObject backBtn = MakeButton(topBar.transform, "Back", new Vector2(12f * s, -28f * s),
            new Vector2(70f * s, 36f * s), new Color(0.08f, 0.15f, 0.35f, 0.9f), new Color(0.75f, 0.80f, 0.95f, 1f),
            12f * s, () => { OnBackToMenu?.Invoke(); });

        // Page title
        GameObject titleObj = new GameObject("PageTitle", typeof(RectTransform));
        titleObj.transform.SetParent(topBar.transform, false);
        TMP_Text titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "Knowledge Base";
        titleTxt.fontSize = 18f * s;
        titleTxt.color = new Color(0.35f, 0.70f, 1f, 1f);
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.fontStyle = FontStyles.Bold;
        if (menuFont != null) titleTxt.font = menuFont;
        RectTransform tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = new Vector2(0, -28f * s); tr.sizeDelta = new Vector2(200f * s, 36f * s);

        // Search input
        GameObject inputObj = new GameObject("SearchInput", typeof(RectTransform), typeof(Image));
        inputObj.transform.SetParent(pagePanel.transform, false);
        RectTransform ir = inputObj.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0, 1); ir.anchorMax = new Vector2(1, 1);
        ir.pivot = new Vector2(0.5f, 1);
        ir.anchoredPosition = new Vector2(0, -64f * s);
        ir.sizeDelta = new Vector2(-24f * s, 44f * s);
        inputObj.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.18f, 0.92f);

        // Search input field
        searchInput = inputObj.AddComponent<TMP_InputField>();
        searchInput.lineType = TMP_InputField.LineType.SingleLine;

        GameObject textArea = new GameObject("TextArea", typeof(RectTransform));
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform tar = textArea.GetComponent<RectTransform>();
        tar.anchorMin = Vector2.zero; tar.anchorMax = Vector2.one;
        tar.offsetMin = new Vector2(10f * s, 0); tar.offsetMax = new Vector2(-10f * s, 0);

        TMP_Text inputText = textArea.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = 14f * s;
        inputText.color = new Color(0.94f, 0.95f, 0.98f, 1f);
        inputText.alignment = TextAlignmentOptions.MidlineLeft;
        if (menuFont != null) inputText.font = menuFont;

        searchInput.textComponent = inputText;

        // Placeholder
        TMP_Text placeholder = textArea.gameObject.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Search...";
        placeholder.fontSize = 14f * s;
        placeholder.color = new Color(0.40f, 0.45f, 0.60f, 0.5f);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        placeholder.fontStyle = FontStyles.Italic;
        if (menuFont != null) placeholder.font = menuFont;
        searchInput.placeholder = placeholder;

        searchInput.onSubmit.AddListener(OnSearchSubmit);

        // Results scroll area
        GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObj.transform.SetParent(pagePanel.transform, false);
        RectTransform sr = scrollObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0, 0); sr.anchorMax = new Vector2(1, 1);
        sr.offsetMin = new Vector2(8f * s, 8f * s);
        sr.offsetMax = new Vector2(-8f * s, -(120f * s));

        scrollObj.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        scrollRect = scrollObj.GetComponent<ScrollRect>();

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform vpr = viewport.GetComponent<RectTransform>();
        vpr.anchorMin = Vector2.zero; vpr.anchorMax = Vector2.one; vpr.sizeDelta = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        // Content
        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(viewport.transform, false);
        contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 4f * s;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = vpr;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    void OnSearchSubmit(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        ClearResults();

        var mgr = FindObjectOfType<AI_Search_Manager>();
        if (mgr == null)
        {
            AddResultItem("Search engine not available.", new Color(0.85f, 0.35f, 0.35f));
            return;
        }

        var matches = mgr.SearchKnowledgeBase(query);
        if (matches == null || matches.Count == 0)
        {
            AddResultItem("No results found for: " + query, new Color(0.75f, 0.78f, 0.85f));
            return;
        }

        AddResultItem("Found " + matches.Count + " result(s):", new Color(0.35f, 0.70f, 1f));

        foreach (var entry in matches)
        {
            // Title
            AddResultItem(entry.title ?? entry.command, new Color(0.35f, 0.70f, 1f));

            // Steps
            if (entry.steps != null && entry.steps.Length > 0)
            {
                for (int i = 0; i < entry.steps.Length; i++)
                {
                    AddResultItem((i + 1) + ". " + entry.steps[i], new Color(0.85f, 0.88f, 0.93f));
                }
            }

            // Separator between results
            if (matches.Count > 1)
            {
                AddResultItem("---", new Color(0.15f, 0.20f, 0.35f));
            }
        }

        // Force layout rebuild and scroll to top
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
        }
    }

    void AddResultItem(string text, Color color)
    {
        float s = Mathf.Clamp(Screen.width / 540f, 1f, 2f);
        GameObject item = new GameObject("ResultItem", typeof(RectTransform), typeof(LayoutElement));
        item.transform.SetParent(contentRect, false);

        LayoutElement le = item.GetComponent<LayoutElement>();
        le.minHeight = 26f * s;

        TMP_Text txt = item.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = 11f * s;
        txt.color = color;
        txt.raycastTarget = false;
        txt.enableWordWrapping = true;
        if (menuFont != null) txt.font = menuFont;

        RectTransform rt = item.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, 26f * s);

        resultItems.Add(item);
    }

    void ClearResults()
    {
        foreach (var item in resultItems) { if (item != null) Destroy(item); }
        resultItems.Clear();
        if (contentRect != null) contentRect.sizeDelta = new Vector2(0, 0);
    }

    GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color bgColor, Color textColor, float fontSize, UnityEngine.Events.UnityAction action)
    {
        GameObject btn = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(parent, false);
        RectTransform br = btn.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(0, 0.5f);
        br.anchoredPosition = pos; br.sizeDelta = size;
        btn.GetComponent<Image>().color = bgColor;
        btn.GetComponent<Button>().onClick.AddListener(action);

        GameObject lbl = new GameObject("Label", typeof(RectTransform));
        lbl.transform.SetParent(btn.transform, false);
        TMP_Text txt = lbl.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = fontSize;
        txt.color = textColor;
        txt.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) txt.font = menuFont;
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.sizeDelta = Vector2.zero;

        return btn;
    }
}

