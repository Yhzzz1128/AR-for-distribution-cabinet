using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ButtonQAPageController : MonoBehaviour
{
    public TMP_FontAsset menuFont;
    public Canvas canvas;
    public System.Action OnBackToMenu;

    private GameObject pagePanel;
    private GameObject detailPanel;
    private TextMeshProUGUI detailText;
    private TextMeshProUGUI statusText;

    private class BtnInfo { public string name; public string answer; }
    private List<BtnInfo> buttons = new List<BtnInfo>();

    [System.Serializable] private class BtnData { public string name; public string answer; }
    [System.Serializable] private class BtnWrapper { public BtnData[] items; }

    public void Show()
    {
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogError("[BtnQA] No Canvas"); return; }
        if (pagePanel != null) { pagePanel.SetActive(true); return; }
        LoadData();
        CreatePage();
    }

    public void Hide() { if (pagePanel != null) pagePanel.SetActive(false); }

    void LoadData()
    {
        buttons.Clear();
        TextAsset json = Resources.Load<TextAsset>("ButtonQA_Data");
        if (json == null) { Debug.LogError("[BtnQA] JSON not found"); return; }
        string wrapped = json.text.Trim().StartsWith("[") ? "{\"items\":" + json.text + "}" : json.text;
        var w = JsonUtility.FromJson<BtnWrapper>(wrapped);
        if (w == null || w.items == null) { Debug.LogError("[BtnQA] JSON fail"); return; }
        foreach (var b in w.items)
            if (b != null) buttons.Add(new BtnInfo { name = b.name ?? "?", answer = b.answer ?? "" });
        Debug.Log("[BtnQA] Loaded " + buttons.Count + " buttons");
    }

    void CreatePage()
    {
        float s = Mathf.Clamp(Screen.width / 540f, 1f, 2f);

        // Background
        pagePanel = MakeGO("Page", canvas.transform);
        Stretch(pagePanel);
        pagePanel.AddComponent<Image>().color = new Color(0.03f, 0.05f, 0.12f, 1f);

        // Top bar
        GameObject topBar = MakeGO("TopBar", pagePanel.transform);
        AnchorPos(topBar, 0, 1, 1, 1, 0, 0, 0, 42f * s);
        topBar.AddComponent<Image>().color = new Color(0.03f, 0.06f, 0.14f, 0.93f);

        // Back
        var bb = MakeBtn(topBar.transform, "<", 8f * s, -21f * s, 36f * s, 30f * s,
            new Color(0.08f, 0.15f, 0.35f, 0.9f), Color.white, 14f * s, () => OnBackToMenu?.Invoke());

        // Title
        var tt = MakeTMP(topBar.transform, "Button Catalog", 14f * s, new Color(0.35f, 0.70f, 1f, 1f));
        tt.alignment = TextAlignmentOptions.Center; tt.fontStyle = FontStyles.Bold;
        AnchorPos(tt.gameObject, 0.5f, 0.5f, 0.5f, 0.5f, 0, -21f * s, 200f * s, 28f * s);

        // Status
        statusText = MakeTMP(pagePanel.transform, buttons.Count + " buttons loaded", 10f * s, new Color(0.4f, 0.45f, 0.55f, 1f));
        statusText.alignment = TextAlignmentOptions.Center;
        AnchorPos(statusText.gameObject, 0.5f, 1f, 0.5f, 1f, 0, -48f * s, 300f * s, 20f * s);

        // === Scroll area ===
        GameObject scrollView = MakeGO("ScrollView", pagePanel.transform);
        AnchorOff(scrollView, 0, 0, 1, 1, 6f * s, 72f * s, 6f * s, 6f * s);
        scrollView.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var scrollRect = scrollView.AddComponent<ScrollRect>();

        GameObject vp = MakeGO("Viewport", scrollView.transform);
        Stretch(vp); vp.AddComponent<Image>().color = new Color(0, 0, 0, 0); vp.AddComponent<RectMask2D>();

        GameObject content = MakeGO("Content", vp.transform);
        AnchorTop(content, 0, 0);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 6, 6); vlg.spacing = 4f * s;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = vp.GetComponent<RectTransform>();
        scrollRect.content = content.GetComponent<RectTransform>();
        scrollRect.horizontal = false; scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Card colors
        Color[] colors = {
            new Color(0.15f, 0.60f, 0.25f, 0.92f), new Color(0.18f, 0.45f, 0.70f, 0.92f),
            new Color(0.20f, 0.70f, 0.30f, 0.92f), new Color(0.80f, 0.25f, 0.15f, 0.92f),
            new Color(0.85f, 0.55f, 0.10f, 0.92f), new Color(0.60f, 0.60f, 0.10f, 0.92f),
            new Color(0.15f, 0.65f, 0.30f, 0.92f), new Color(0.75f, 0.20f, 0.10f, 0.92f),
            new Color(0.15f, 0.55f, 0.25f, 0.92f)
        };

        for (int i = 0; i < buttons.Count; i++)
        {
            int idx = i; var info = buttons[i];

            GameObject card = MakeGO("Card" + i, content.transform);
            card.AddComponent<Image>().color = i < colors.Length ? colors[i] : Color.gray;
            card.AddComponent<Button>().onClick.AddListener(() => ShowDetail(idx));
            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 40f * s; le.preferredHeight = 40f * s;

            GameObject label = MakeGO("Lbl", card.transform);
            var lt = label.AddComponent<TextMeshProUGUI>();
            lt.text = (i + 1) + ". " + info.name;
            lt.fontSize = 13f * s; lt.color = Color.white;
            lt.alignment = TextAlignmentOptions.Center; lt.fontStyle = FontStyles.Bold;
            if (menuFont != null) lt.font = menuFont;
            Stretch(label);
        }

        // === Detail overlay ===
        detailPanel = MakeGO("DetailPanel", pagePanel.transform);
        detailPanel.transform.SetAsLastSibling();
        detailPanel.AddComponent<Image>().color = new Color(0.03f, 0.06f, 0.16f, 0.97f);
        AnchorOff(detailPanel, 0.04f, 0.8f, 0.96f, 0.04f);

        // Close X
        MakeBtn(detailPanel.transform, "X", -32f * s, -10f * s, 28f * s, 28f * s,
            new Color(0.3f, 0.1f, 0.1f, 0.9f), Color.white, 14f * s, () => detailPanel.SetActive(false));

        // Detail text
        var dt = detailPanel.AddComponent<TextMeshProUGUI>();
        detailText = dt;
        dt.fontSize = 12f * s; dt.color = new Color(0.88f, 0.9f, 0.95f, 1f);
        dt.alignment = TextAlignmentOptions.TopLeft;
        if (menuFont != null) dt.font = menuFont;
        AnchorOff(dt.gameObject, 0, 1, 1, 0, 14f * s, 50f * s, 14f * s, 14f * s);

        detailPanel.SetActive(false);
        Debug.Log("[BtnQA] Done - " + buttons.Count + " cards");
    }

    void ShowDetail(int index)
    {
        if (detailText == null || detailPanel == null || index < 0 || index >= buttons.Count) return;
        detailText.text = buttons[index].answer ?? "";
        detailPanel.SetActive(true);
    }

    // ===== Helpers =====
    GameObject MakeGO(string n, Transform p) { var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false); return g; }

    TextMeshProUGUI MakeTMP(Transform p, string t, float fs, Color c)
    {
        var g = MakeGO("T", p); var txt = g.AddComponent<TextMeshProUGUI>();
        txt.text = t; txt.fontSize = fs; txt.color = c;
        if (menuFont != null) txt.font = menuFont;
        return txt;
    }

    GameObject MakeBtn(Transform p, string t, float x, float y, float w, float h, Color bg, Color tc, float fs, System.Action a)
    {
        var g = MakeGO("B", p); g.AddComponent<Image>().color = bg;
        g.AddComponent<Button>().onClick.AddListener(() => a());
        AnchorPos(g, 0, 0.5f, 0, 0.5f, x, y, w, h);
        var lbl = MakeTMP(g.transform, t, fs, tc); lbl.alignment = TextAlignmentOptions.Center;
        Stretch(lbl.gameObject);
        return g;
    }

    void Stretch(GameObject g) { var r = g.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    void AnchorPos(GameObject g, float ax, float ay, float px, float py, float x, float y, float w, float h)
    {
        var r = g.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(ax, ay); r.anchorMax = new Vector2(px, py);
        r.pivot = new Vector2(ax, ay);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    void AnchorTop(GameObject g, float w, float h)
    {
        var r = g.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(0.5f, 1); r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(w, h);
    }

    void AnchorOff(GameObject g, float ax, float ay, float px, float py, float l, float t, float r, float b)
    {
        var rt = g.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(px, py);
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
    }
}