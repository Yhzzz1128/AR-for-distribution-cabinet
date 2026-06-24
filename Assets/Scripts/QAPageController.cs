using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class QAPageController : MonoBehaviour
{
    public TMP_FontAsset menuFont;
    public Canvas canvas;
    public Action OnBackToMenu;

    private GameObject pagePanel;
    private TMP_InputField searchInput;
    private RectTransform contentRect;
    private ScrollRect scrollRect;
    private List<GameObject> resultItems = new List<GameObject>();

    // Knowledge bases
    private List<KBEntry> operationKB = new List<KBEntry>();
    private List<KBEntry> generalKB = new List<KBEntry>();
    private TMP_Text statusText;

    [Serializable]
    public class KBEntry
    {
        public string title;
        public string category;
        public string[] keywords;
        public string content;
        public string[] steps;
        public string command;
        public string source;
    }

    [Serializable]
    public class KBWrapper { public KBEntry[] items; }

    // Local wrapper for OperationData.json (avoids dependency on AI_Search_Manager types)
    [Serializable]
    public class OpDataEntry { public string command; public string title; public string[] keywords; public string[] steps; }
    [Serializable]
    public class OpDataWrapper { public OpDataEntry[] items; }

    public void Show()
    {
        if (menuFont == null)
        {
            var mgr = FindObjectOfType<AI_Search_Manager>();
            if (mgr != null && mgr.chineseFont != null) menuFont = mgr.chineseFont;
        }
        if (pagePanel != null) { pagePanel.SetActive(true); return; }
        LoadKnowledgeBases();
        CreatePage();
    }

    public void Hide()
    {
        if (pagePanel != null) pagePanel.SetActive(false);
    }

    void LoadKnowledgeBases()
    {
        TextAsset opJson = Resources.Load<TextAsset>("OperationData");
        if (opJson != null)
        {
            string wrapped = opJson.text.Trim().StartsWith("[") ? "{\"items\":" + opJson.text + "}" : opJson.text;
            var wrapper = JsonUtility.FromJson<OpDataWrapper>(wrapped);
            if (wrapper != null && wrapper.items != null)
            {
                Debug.Log("[QAPage] Loaded " + wrapper.items.Length + " operation entries");
                foreach (var e in wrapper.items)
                {
                    operationKB.Add(new KBEntry
                    {
                        title = e.title, command = e.command, keywords = e.keywords,
                        steps = e.steps, source = "操作流程"
                    });
                }
            }
        }

        TextAsset gkJson = Resources.Load<TextAsset>("PDG_Knowledge");
        if (gkJson != null)
        {
            string wrapped = gkJson.text.Trim().StartsWith("[") ? "{\"items\":" + gkJson.text + "}" : gkJson.text;
            var wrapper = JsonUtility.FromJson<KBWrapper>(wrapped);
            if (wrapper != null && wrapper.items != null)
            {
                Debug.Log("[QAPage] Loaded " + wrapper.items.Length + " general knowledge entries");
                foreach (var e in wrapper.items)
                {
                    generalKB.Add(new KBEntry
                    {
                        title = e.title, category = e.category, keywords = e.keywords,
                        content = e.content, source = "知识百科"
                    });
                }
            }
        }
    }

    void CreatePage()
    {
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        float s = Mathf.Clamp(Screen.width / 540f, 1f, 2f);

        pagePanel = MakeFullPanel(s);

        // Top bar
        GameObject topBar = MakeTopBar(pagePanel.transform, s);
        MakeBackButton(topBar.transform, s);

        // Title
        MakeTitle(topBar.transform, s);

        // Search input
        MakeSearchInput(pagePanel.transform, s);

        // Results area
        MakeResultsArea(pagePanel.transform, s);
    }

    GameObject MakeFullPanel(float s)
    {
        GameObject p = new GameObject("QAPage", typeof(RectTransform), typeof(Image));
        p.transform.SetParent(canvas.transform, false);
        p.transform.SetAsLastSibling();
        RectTransform pr = p.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        p.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.12f, 1f);
        return p;
    }

    GameObject MakeTopBar(Transform parent, float s)
    {
        GameObject bar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(parent, false);
        RectTransform tb = bar.GetComponent<RectTransform>();
        tb.anchorMin = new Vector2(0, 1); tb.anchorMax = new Vector2(1, 1);
        tb.pivot = new Vector2(0.5f, 1);
        tb.sizeDelta = new Vector2(0, 56f * s); tb.anchoredPosition = Vector2.zero;
        bar.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.18f, 0.95f);
        return bar;
    }

    void MakeBackButton(Transform parent, float s)
    {
        MakeBtn(parent, "Back", new Vector2(12f * s, -28f * s), new Vector2(70f * s, 36f * s),
            new Color(0.08f, 0.15f, 0.35f, 0.9f), new Color(0.75f, 0.80f, 0.95f, 1f), 12f * s,
            () => OnBackToMenu?.Invoke());
    }

    void MakeTitle(Transform parent, float s)
    {
        GameObject obj = new GameObject("PageTitle", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        TMP_Text txt = obj.AddComponent<TextMeshProUGUI>();
        txt.text = "Knowledge Base";
        txt.fontSize = 18f * s;
        txt.color = new Color(0.35f, 0.70f, 1f, 1f);
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold;
        if (menuFont != null) txt.font = menuFont;
        RectTransform tr = obj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = new Vector2(0, -28f * s); tr.sizeDelta = new Vector2(200f * s, 36f * s);
    }

    void MakeSearchInput(Transform parent, float s)
    {
        GameObject inputObj = new GameObject("SearchInput", typeof(RectTransform), typeof(Image));
        inputObj.transform.SetParent(parent, false);
        RectTransform ir = inputObj.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0, 1); ir.anchorMax = new Vector2(1, 1);
        ir.pivot = new Vector2(0.5f, 1);
        ir.anchoredPosition = new Vector2(0, -64f * s); ir.sizeDelta = new Vector2(-24f * s, 44f * s);
        inputObj.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.18f, 0.92f);

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

        TMP_Text placeholder = textArea.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Search...";
        placeholder.fontSize = 14f * s;
        placeholder.color = new Color(0.40f, 0.45f, 0.60f, 0.5f);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        placeholder.fontStyle = FontStyles.Italic;
        if (menuFont != null) placeholder.font = menuFont;
        searchInput.placeholder = placeholder;

        searchInput.onSubmit.AddListener(OnSearchSubmit);
        searchInput.onEndEdit.AddListener(OnSearchSubmit);

        // Hint
        GameObject hintObj = new GameObject("SearchHint", typeof(RectTransform));
        hintObj.transform.SetParent(parent, false);
        TMP_Text hintTxt = hintObj.AddComponent<TextMeshProUGUI>();
        hintTxt.text = "Press Enter to search";
        hintTxt.fontSize = 10f * s; hintTxt.color = new Color(0.35f, 0.40f, 0.55f, 0.7f);
        hintTxt.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) hintTxt.font = menuFont;
        RectTransform hr = hintObj.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0, 1); hr.anchorMax = new Vector2(1, 1);
        hr.pivot = new Vector2(0.5f, 1);
        hr.anchoredPosition = new Vector2(0, -112f * s); hr.sizeDelta = new Vector2(-24f * s, 18f * s);

        // Status
        GameObject statObj = new GameObject("KBStatus", typeof(RectTransform));
        statObj.transform.SetParent(parent, false);
        statusText = statObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Loaded: " + (operationKB.Count + generalKB.Count) + " entries";
        statusText.fontSize = 10f * s; statusText.color = new Color(0.30f, 0.60f, 0.40f, 0.7f);
        statusText.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) statusText.font = menuFont;
        RectTransform sr2 = statObj.GetComponent<RectTransform>();
        sr2.anchorMin = new Vector2(0, 1); sr2.anchorMax = new Vector2(1, 1);
        sr2.pivot = new Vector2(0.5f, 1);
        sr2.anchoredPosition = new Vector2(0, -132f * s); sr2.sizeDelta = new Vector2(-24f * s, 18f * s);
    }

    void MakeResultsArea(Transform parent, float s)
    {
        GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObj.transform.SetParent(parent, false);
        RectTransform sr = scrollObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0, 0); sr.anchorMax = new Vector2(1, 1);
        sr.offsetMin = new Vector2(8f * s, 8f * s);
        sr.offsetMax = new Vector2(-8f * s, -(120f * s));
        scrollObj.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        scrollRect = scrollObj.GetComponent<ScrollRect>();

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform vpr = viewport.GetComponent<RectTransform>();
        vpr.anchorMin = Vector2.zero; vpr.anchorMax = Vector2.one; vpr.sizeDelta = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(viewport.transform, false);
        contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero; contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4); layout.spacing = 4f * s;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = vpr;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false; scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    // ===== Search =====

        void OnSearchSubmit(string query)
    {
        Debug.Log("[QAPage] Search submitted: '" + query + "' ops=" + operationKB.Count + " gen=" + generalKB.Count);

        ClearResults();

        if (string.IsNullOrWhiteSpace(query))
        {
            ShowFeedback("搜索框为空或没有搜索到相关内容");
            return;
        }

        string q = NormalizeText(query);
        var scored = new List<(KBEntry entry, int score, string source)>();

        foreach (var e in operationKB)
        {
            int s = ScoreOpEntry(q, e);
            if (s >= 1) scored.Add((e, s, e.source));
        }

        foreach (var e in generalKB)
        {
            int s = ScoreGeneralEntry(q, e);
            if (s >= 1) scored.Add((e, s, e.source));
        }

        Debug.Log("[QAPage] Scored: " + scored.Count);

        if (scored.Count == 0)
        {
            ShowFeedback("搜索框为空或没有搜索到相关内容");
            return;
        }

        scored.Sort((a, b) => b.score.CompareTo(a.score));
        AddResultItem("Found " + scored.Count + " result(s):", new Color(0.35f, 0.70f, 1f));

        foreach (var (entry, _, src) in scored)
        {
            AddResultItem("[" + src + "] " + (entry.title ?? entry.command), new Color(0.35f, 0.70f, 1f));
            if (entry.steps != null && entry.steps.Length > 0)
            {
                for (int i = 0; i < entry.steps.Length; i++)
                    AddResultItem((i + 1) + ". " + entry.steps[i], new Color(0.85f, 0.88f, 0.93f));
            }
            else if (!string.IsNullOrWhiteSpace(entry.content))
            {
                AddResultItem(entry.content, new Color(0.82f, 0.85f, 0.90f));
            }
            if (entry.category != null)
                AddResultItem("  Category: " + entry.category, new Color(0.40f, 0.45f, 0.55f));
            if (scored.Count > 1)
                AddResultItem("---", new Color(0.15f, 0.20f, 0.35f));
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        if (contentRect != null && contentRect.parent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect.parent as RectTransform);
        if (scrollRect != null) { scrollRect.verticalNormalizedPosition = 1f; Canvas.ForceUpdateCanvases(); }
    }

    void ShowFeedback(string msg)
    {
        AddResultItem(msg, new Color(1f, 0.65f, 0.3f, 1f));
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    int ScoreOpEntry(string query, KBEntry entry)
    {
        int score = 0;
        string searchable = NormalizeText((entry.command ?? "") + " " + (entry.title ?? "") + " " +
            string.Join(" ", entry.keywords ?? Array.Empty<string>()) + " " +
            string.Join(" ", entry.steps ?? Array.Empty<string>()));

        if (!string.IsNullOrWhiteSpace(entry.command) &&
            (query.Contains(NormalizeText(entry.command)) || NormalizeText(entry.command).Contains(query)))
            score += 6;
        if (!string.IsNullOrWhiteSpace(entry.title) &&
            (query.Contains(NormalizeText(entry.title)) || NormalizeText(entry.title).Contains(query)))
            score += 4;

        foreach (string token in SplitTokens(query))
            if (token.Length >= 2 && searchable.Contains(token)) score += 1;

        return score;
    }

    int ScoreGeneralEntry(string query, KBEntry entry)
    {
        int score = 0;
        string searchable = NormalizeText((entry.title ?? "") + " " + (entry.category ?? "") + " " +
            string.Join(" ", entry.keywords ?? Array.Empty<string>()) + " " + (entry.content ?? ""));

        if (!string.IsNullOrWhiteSpace(entry.title) &&
            (query.Contains(NormalizeText(entry.title)) || NormalizeText(entry.title).Contains(query)))
            score += 4;

        foreach (string token in SplitTokens(query))
            if (token.Length >= 2 && searchable.Contains(token)) score += 1;

        return score;
    }

    string NormalizeText(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        StringBuilder sb = new StringBuilder(input.Length);
        foreach (char c in input)
            if (!char.IsControl(c) || c == '\n') sb.Append(c);
        return sb.ToString().Trim().ToLowerInvariant();
    }

    IEnumerable<string> SplitTokens(string text)
    {
        char[] sep = { ' ', ',', '，', '.', '。', '?', '？', '!', '！', ';', '；', ':', '：', '\n', '\r', '\t' };
        foreach (string token in text.Split(sep, StringSplitOptions.RemoveEmptyEntries))
            yield return token.Trim();
    }

    // ===== UI Helpers =====

    void AddResultItem(string text, Color color)
    {
        float s = Mathf.Clamp(Screen.width / 540f, 1f, 2f);
        GameObject item = new GameObject("ResultItem", typeof(RectTransform), typeof(LayoutElement));
        item.transform.SetParent(contentRect, false);
        LayoutElement le = item.GetComponent<LayoutElement>();
        le.minHeight = 26f * s;

        TMP_Text txt = item.AddComponent<TextMeshProUGUI>();
        txt.text = text; txt.fontSize = 11f * s; txt.color = color;
        txt.raycastTarget = false; txt.enableWordWrapping = true;
        if (menuFont != null) txt.font = menuFont;

        RectTransform rt = item.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1); rt.sizeDelta = new Vector2(0, 26f * s);
        resultItems.Add(item);
    }

    void ClearResults()
    {
        foreach (var item in resultItems) { if (item != null) Destroy(item); }
        resultItems.Clear();
        if (contentRect != null) contentRect.sizeDelta = Vector2.zero;
    }

    void MakeBtn(Transform parent, string label, Vector2 pos, Vector2 size,
        Color bg, Color tc, float fs, Action action)
    {
        GameObject btn = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(parent, false);
        RectTransform br = btn.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(0, 0.5f);
        br.anchoredPosition = pos; br.sizeDelta = size;
        btn.GetComponent<Image>().color = bg;
        btn.GetComponent<Button>().onClick.AddListener(() => action());

        GameObject lbl = new GameObject("Label", typeof(RectTransform));
        lbl.transform.SetParent(btn.transform, false);
        TMP_Text txt = lbl.AddComponent<TextMeshProUGUI>();
        txt.text = label; txt.fontSize = fs; txt.color = tc;
        txt.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) txt.font = menuFont;
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.sizeDelta = Vector2.zero;
    }
}






