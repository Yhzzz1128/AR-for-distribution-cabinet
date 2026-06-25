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

    private class BtnInfo { public string name; public string answer; public string[] keywords; }
    private List<BtnInfo> buttons = new List<BtnInfo>();

    [System.Serializable]
    private class BtnData { public string name; public string answer; public string[] keywords; }
    [System.Serializable]
    private class BtnWrapper { public BtnData[] items; }

    public void Show()
    {
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        if (pagePanel != null) { pagePanel.SetActive(true); return; }
        LoadData();
        CreatePage();
    }

    public void Hide()
    {
        if (pagePanel != null) pagePanel.SetActive(false);
        if (detailOverlay != null) detailOverlay.SetActive(false);
    }

    void LoadData()
    {
        buttons.Clear();
        TextAsset json = Resources.Load<TextAsset>("ButtonQA_Data");
        if (json != null)
        {
            string wrapped = json.text.Trim().StartsWith("[") ? "{\"items\":" + json.text + "}" : json.text;
            var w = JsonUtility.FromJson<BtnWrapper>(wrapped);
            if (w != null && w.items != null)
            {
                foreach (var b in w.items)
                    buttons.Add(new BtnInfo { name = b.name, answer = b.answer, keywords = b.keywords });
            }
        }
    }

    void CreatePage()
    {
        float s = Mathf.Clamp(Screen.width / 540f, 1f, 2f);

        pagePanel = new GameObject("ButtonQAPage", typeof(RectTransform), typeof(Image));
        pagePanel.transform.SetParent(canvas.transform, false);
        pagePanel.transform.SetAsLastSibling();
        RectTransform pr = pagePanel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        pagePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);

        // Top bar
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(pagePanel.transform, false);
        RectTransform tb = topBar.GetComponent<RectTransform>();
        tb.anchorMin = new Vector2(0, 1); tb.anchorMax = new Vector2(1, 1);
        tb.pivot = new Vector2(0.5f, 1);
        tb.sizeDelta = new Vector2(0, 40f * s); tb.anchoredPosition = Vector2.zero;
        topBar.GetComponent<Image>().color = new Color(0.03f, 0.06f, 0.14f, 0.93f);

        MakeTMPBtn(topBar.transform, "<", new Vector2(8f * s, -20f * s), new Vector2(38f * s, 28f * s),
            new Color(0.08f, 0.15f, 0.35f, 0.9f), new Color(0.75f, 0.80f, 0.95f, 1f), 14f * s,
            () => OnBackToMenu?.Invoke());

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(topBar.transform, false);
        TMP_Text ttxt = titleObj.AddComponent<TextMeshProUGUI>();
        ttxt.text = "Button Catalog"; ttxt.fontSize = 14f * s;
        ttxt.color = new Color(0.35f, 0.70f, 1f, 1f); ttxt.alignment = TextAlignmentOptions.Center;
        ttxt.fontStyle = FontStyles.Bold;
        if (menuFont != null) ttxt.font = menuFont;
        RectTransform tr2 = titleObj.GetComponent<RectTransform>();
        tr2.anchorMin = tr2.anchorMax = tr2.pivot = new Vector2(0.5f, 0.5f);
        tr2.anchoredPosition = new Vector2(0, -20f * s); tr2.sizeDelta = new Vector2(200f * s, 28f * s);

        // Scrollable grid of button cards
        GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObj.transform.SetParent(pagePanel.transform, false);
        RectTransform sr = scrollObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0, 0); sr.anchorMax = new Vector2(1, 1);
        sr.offsetMin = new Vector2(6f * s, 6f * s);
        sr.offsetMax = new Vector2(-6f * s, -(46f * s));
        scrollObj.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform vp = viewport.GetComponent<RectTransform>();
        vp.anchorMin = Vector2.zero; vp.anchorMax = Vector2.one; vp.sizeDelta = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform cr = content.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1);
        cr.pivot = new Vector2(0.5f, 1); cr.anchoredPosition = Vector2.zero;
        cr.sizeDelta = new Vector2(0, buttons.Count * (72f * s + 6f * s) + 6f * s);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4); layout.spacing = 6f * s;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true; layout.childControlHeight = false;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;

        scrollRect.viewport = vp; scrollRect.content = cr;
        scrollRect.horizontal = false; scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Create button cards
        for (int i = 0; i < buttons.Count; i++)
        {
            int idx = i;
            BtnInfo info = buttons[i];

            GameObject card = new GameObject("BtnCard_" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            card.transform.SetParent(content.transform, false);
            LayoutElement le = card.GetComponent<LayoutElement>();
            le.minHeight = 72f * s; le.preferredHeight = 72f * s;

            Image cardImg = card.GetComponent<Image>();
            // Alternate colors like real panel buttons
            Color[] colors = { new Color(0.12f, 0.25f, 0.45f, 0.88f), new Color(0.10f, 0.22f, 0.38f, 0.88f),
                               new Color(0.08f, 0.35f, 0.25f, 0.85f), new Color(0.30f, 0.18f, 0.08f, 0.85f) };
            cardImg.color = colors[i % colors.Length];
            card.GetComponent<Button>().onClick.AddListener(() => ShowDetail(idx));

            // Card name label
            GameObject nameObj = new GameObject("Name", typeof(RectTransform));
            nameObj.transform.SetParent(card.transform, false);
            Text nameTxt = nameObj.AddComponent<Text>();
            nameTxt.text = (i + 1) + ". " + info.name;
            nameTxt.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            nameTxt.fontSize = 15; nameTxt.color = Color.white;
            nameTxt.alignment = TextAnchor.MiddleLeft; nameTxt.fontStyle = FontStyle.Bold;
            RectTransform nr = nameObj.GetComponent<RectTransform>();
            nr.anchorMin = Vector2.zero; nr.anchorMax = Vector2.one;
            nr.offsetMin = new Vector2(14f * s, 38f * s); nr.offsetMax = new Vector2(-10f * s, -4f * s);

            // Brief preview
            GameObject prevObj = new GameObject("Preview", typeof(RectTransform));
            prevObj.transform.SetParent(card.transform, false);
            Text prevTxt = prevObj.AddComponent<Text>();
            string preview = info.answer;
            if (preview.Length > 40) preview = preview.Substring(0, 40) + "...";
            prevTxt.text = preview;
            prevTxt.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
            prevTxt.fontSize = 11; prevTxt.color = new Color(0.7f, 0.75f, 0.85f, 1f);
            prevTxt.alignment = TextAnchor.MiddleLeft;
            RectTransform pvr = prevObj.GetComponent<RectTransform>();
            pvr.anchorMin = Vector2.zero; pvr.anchorMax = Vector2.one;
            pvr.offsetMin = new Vector2(14f * s, 4f * s); pvr.offsetMax = new Vector2(-10f * s, -34f * s);
        }

        // Detail overlay (initially hidden)
        detailOverlay = new GameObject("DetailOverlay", typeof(RectTransform), typeof(Image));
        detailOverlay.transform.SetParent(pagePanel.transform, false);
        detailOverlay.transform.SetAsLastSibling();
        RectTransform dor = detailOverlay.GetComponent<RectTransform>();
        dor.anchorMin = new Vector2(0.05f, 0.08f); dor.anchorMax = new Vector2(0.95f, 0.92f);
        dor.offsetMin = dor.offsetMax = Vector2.zero;
        detailOverlay.GetComponent<Image>().color = new Color(0.03f, 0.06f, 0.16f, 0.97f);

        // Close button on overlay
        MakeTMPBtn(detailOverlay.transform, "X", new Vector2(-12f * s, -12f * s), new Vector2(32f * s, 32f * s),
            new Color(0.3f, 0.1f, 0.1f, 0.9f), Color.white, 16f * s,
            () => detailOverlay.SetActive(false));
        // Anchor close button to top-right
        RectTransform cbr = detailOverlay.transform.GetChild(0).GetComponent<RectTransform>();
        cbr.anchorMin = cbr.anchorMax = cbr.pivot = new Vector2(1, 1);

        detailText = detailOverlay.AddComponent<Text>();
        detailText.font = Font.CreateDynamicFontFromOSFont("Arial", 13);
        detailText.fontSize = 13; detailText.color = new Color(0.88f, 0.9f, 0.95f, 1f);
        detailText.alignment = TextAnchor.UpperLeft;
        RectTransform dtr = detailText.GetComponent<RectTransform>();
        dtr.anchorMin = Vector2.zero; dtr.anchorMax = Vector2.one;
        dtr.offsetMin = new Vector2(16f * s, 16f * s); dtr.offsetMax = new Vector2(-16f * s, -50f * s);

        detailOverlay.SetActive(false);
    }

    void ShowDetail(int index)
    {
        if (index < 0 || index >= buttons.Count) return;
        detailText.text = buttons[index].answer;
        detailOverlay.SetActive(true);
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
        GameObject lbl = new GameObject("Lbl", typeof(RectTransform));
        lbl.transform.SetParent(btn.transform, false);
        TMP_Text txt = lbl.AddComponent<TextMeshProUGUI>();
        txt.text = label; txt.fontSize = fs; txt.color = tc;
        txt.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) txt.font = menuFont;
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.sizeDelta = Vector2.zero;
    }
}