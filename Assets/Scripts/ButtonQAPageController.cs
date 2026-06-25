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
    private GameObject detailOverlay;
    private Text detailText;
    private Font fallbackFont;

    private class BtnInfo { public string name; public string answer; public string[] keywords; }
    private List<BtnInfo> buttons = new List<BtnInfo>();

    [System.Serializable] private class BtnData { public string name; public string answer; public string[] keywords; }
    [System.Serializable] private class BtnWrapper { public BtnData[] items; }

    public void Show()
    {
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        if (pagePanel != null) { pagePanel.SetActive(true); return; }
        LoadData();
        fallbackFont = Font.CreateDynamicFontFromOSFont("Arial", 13);
        CreatePage();
    }

    public void Hide() { if (pagePanel != null) pagePanel.SetActive(false); if (detailOverlay != null) detailOverlay.SetActive(false); }

    void LoadData()
    {
        buttons.Clear();
        TextAsset json = Resources.Load<TextAsset>("ButtonQA_Data");
        if (json != null)
        {
            string wrapped = json.text.Trim().StartsWith("[") ? "{\"items\":" + json.text + "}" : json.text;
            var w = JsonUtility.FromJson<BtnWrapper>(wrapped);
            if (w != null && w.items != null)
                foreach (var b in w.items)
                    if (b != null) buttons.Add(new BtnInfo { name = b.name ?? "", answer = b.answer ?? "", keywords = b.keywords });
        }
    }

    void CreatePage()
    {
        float s = Mathf.Clamp(Screen.width / 540f, 1f, 2f);

        pagePanel = new GameObject("ButtonQAPage", typeof(RectTransform), typeof(Image));
        pagePanel.transform.SetParent(canvas.transform, false);
        pagePanel.transform.SetAsLastSibling();
        RectTransform pr = pagePanel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.offsetMin = pr.offsetMax = Vector2.zero;
        pagePanel.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.12f, 1f);

        // Top bar
        GameObject topBar = MakeRect("TopBar", pagePanel.transform);
        RectTransform tb = topBar.GetComponent<RectTransform>();
        tb.anchorMin = new Vector2(0, 1); tb.anchorMax = new Vector2(1, 1);
        tb.pivot = new Vector2(0.5f, 1); tb.sizeDelta = new Vector2(0, 40f * s); tb.anchoredPosition = Vector2.zero;
        topBar.AddComponent<Image>().color = new Color(0.03f, 0.06f, 0.14f, 0.93f);

        // Back button
        MakeTMPBtn(topBar.transform, "<", new Vector2(8f * s, -20f * s), new Vector2(38f * s, 28f * s),
            new Color(0.08f, 0.15f, 0.35f, 0.9f), new Color(0.75f, 0.80f, 0.95f, 1f), 14f * s, () => OnBackToMenu?.Invoke());

        // Title
        GameObject titleObj = MakeRect("Title", topBar.transform);
        TMP_Text ttxt = titleObj.AddComponent<TextMeshProUGUI>();
        ttxt.text = "Button Catalog"; ttxt.fontSize = 14f * s; ttxt.color = new Color(0.35f, 0.70f, 1f, 1f);
        ttxt.alignment = TextAlignmentOptions.Center; ttxt.fontStyle = FontStyles.Bold;
        if (menuFont != null) ttxt.font = menuFont;
        RectTransform tr2 = titleObj.GetComponent<RectTransform>();
        tr2.anchorMin = tr2.anchorMax = tr2.pivot = new Vector2(0.5f, 0.5f);
        tr2.anchoredPosition = new Vector2(0, -20f * s); tr2.sizeDelta = new Vector2(200f * s, 28f * s);

        // Scroll area
        GameObject scrollObj = MakeRect("ScrollView", pagePanel.transform);
        scrollObj.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        RectTransform sr = scrollObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0, 0); sr.anchorMax = new Vector2(1, 1);
        sr.offsetMin = new Vector2(6f * s, 6f * s); sr.offsetMax = new Vector2(-6f * s, -(46f * s));

        GameObject viewport = MakeRect("Viewport", scrollObj.transform);
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewport.AddComponent<RectMask2D>();
        RectTransform vp = viewport.GetComponent<RectTransform>();
        vp.anchorMin = Vector2.zero; vp.anchorMax = Vector2.one; vp.sizeDelta = Vector2.zero;

        GameObject content = MakeRect("Content", viewport.transform);
        RectTransform cr = content.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1);
        cr.pivot = new Vector2(0.5f, 1); cr.anchoredPosition = Vector2.zero;
        cr.sizeDelta = new Vector2(0, Mathf.Max(1, buttons.Count) * (46f * s + 6f * s) + 10f * s);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4); layout.spacing = 6f * s;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true; layout.childControlHeight = false;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;

        scrollRect.viewport = vp; scrollRect.content = cr;
        scrollRect.horizontal = false; scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Create detail overlay FIRST (before cards, so it always exists)
        detailOverlay = MakeRect("DetailOverlay", pagePanel.transform);
        detailOverlay.AddComponent<Image>().color = new Color(0.03f, 0.06f, 0.16f, 0.97f);
        detailOverlay.transform.SetAsLastSibling();
        RectTransform dor = detailOverlay.GetComponent<RectTransform>();
        dor.anchorMin = new Vector2(0.05f, 0.08f); dor.anchorMax = new Vector2(0.95f, 0.92f);
        dor.offsetMin = dor.offsetMax = Vector2.zero;

        // Close X button
        MakeTMPBtn(detailOverlay.transform, "X", new Vector2(-12f * s, -12f * s), new Vector2(32f * s, 32f * s),
            new Color(0.3f, 0.1f, 0.1f, 0.9f), Color.white, 16f * s, () => detailOverlay.SetActive(false));
        if (detailOverlay.transform.childCount > 0)
        {
            RectTransform cbr = detailOverlay.transform.GetChild(0).GetComponent<RectTransform>();
            if (cbr != null) { cbr.anchorMin = cbr.anchorMax = cbr.pivot = new Vector2(1, 1); }
        }

        detailText = detailOverlay.AddComponent<Text>();
        if (fallbackFont != null) detailText.font = fallbackFont;
        detailText.fontSize = 13; detailText.color = new Color(0.88f, 0.9f, 0.95f, 1f);
        detailText.alignment = TextAnchor.UpperLeft;
        RectTransform dtr = detailText.GetComponent<RectTransform>();
        dtr.anchorMin = Vector2.zero; dtr.anchorMax = Vector2.one;
        dtr.offsetMin = new Vector2(16f * s, 16f * s); dtr.offsetMax = new Vector2(-16f * s, -50f * s);
        detailOverlay.SetActive(false);

        // Now create button cards
        Color[] btnColors = {
            new Color(0.15f, 0.60f, 0.25f, 0.92f), // green - power/start
            new Color(0.18f, 0.45f, 0.70f, 0.92f), // blue - mode selector
            new Color(0.15f, 0.65f, 0.30f, 0.92f), // green - open valve
            new Color(0.80f, 0.25f, 0.15f, 0.92f), // red - close valve
            new Color(0.85f, 0.55f, 0.10f, 0.92f), // orange - stop
            new Color(0.60f, 0.60f, 0.10f, 0.92f), // yellow - indicator
            new Color(0.15f, 0.65f, 0.30f, 0.92f), // green - open position
            new Color(0.75f, 0.20f, 0.10f, 0.92f), // red - close running
            new Color(0.15f, 0.55f, 0.25f, 0.92f), // green - open running
        };

        for (int i = 0; i < buttons.Count; i++)
        {
            int idx = i;
            BtnInfo info = buttons[i];
            if (info == null) continue;

            GameObject card = new GameObject("BtnCard_" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            card.transform.SetParent(content.transform, false);
            LayoutElement le = card.GetComponent<LayoutElement>();
            le.minHeight = 46f * s; le.preferredHeight = 46f * s;

            card.GetComponent<Image>().color = (i < btnColors.Length) ? btnColors[i] : new Color(0.12f, 0.25f, 0.45f, 0.88f);
            card.GetComponent<Button>().onClick.AddListener(() => ShowDetail(idx));

            // Name label (centered in card)
            GameObject nameObj = MakeRect("Name", card.transform);
            Text nameTxt = nameObj.AddComponent<Text>();
            nameTxt.text = (info.name ?? "Unknown");
            if (fallbackFont != null) nameTxt.font = fallbackFont;
            nameTxt.fontSize = 14; nameTxt.color = Color.white;
            nameTxt.alignment = TextAnchor.MiddleCenter; nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform nr = nameObj.GetComponent<RectTransform>();
            nr.anchorMin = Vector2.zero; nr.anchorMax = Vector2.one; nr.offsetMin = nr.offsetMax = Vector2.zero;
        }
    }

    void ShowDetail(int index)
    {
        if (detailText == null || detailOverlay == null) return;
        if (index < 0 || index >= buttons.Count) return;
        var info = buttons[index];
        if (info == null || string.IsNullOrEmpty(info.answer)) return;
        detailText.text = info.answer;
        detailOverlay.SetActive(true);
    }

    GameObject MakeRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    void MakeTMPBtn(Transform parent, string label, Vector2 pos, Vector2 size, Color bg, Color tc, float fs, System.Action action)
    {
        GameObject btn = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(parent, false);
        RectTransform br = btn.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(0, 0.5f);
        br.anchoredPosition = pos; br.sizeDelta = size;
        btn.GetComponent<Image>().color = bg;
        btn.GetComponent<Button>().onClick.AddListener(() => action());

        GameObject lbl = MakeRect("Lbl", btn.transform);
        TMP_Text txt = lbl.AddComponent<TextMeshProUGUI>();
        txt.text = label; txt.fontSize = fs; txt.color = tc;
        txt.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) txt.font = menuFont;
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.sizeDelta = Vector2.zero;
    }
}