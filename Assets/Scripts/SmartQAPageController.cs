using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SmartQAPageController : MonoBehaviour
{
    public TMP_FontAsset menuFont;
    public Canvas canvas;
    public System.Action OnBackToMenu;

    private GameObject pagePanel;
    private SmartQA smartQA;

    public void Show()
    {
        if (menuFont == null)
        {
            var mgr = FindObjectOfType<AI_Search_Manager>();
            if (mgr != null && mgr.chineseFont != null) menuFont = mgr.chineseFont;
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

        // Full-screen panel
        pagePanel = new GameObject("SmartQAPage", typeof(RectTransform), typeof(Image));
        pagePanel.transform.SetParent(canvas.transform, false);
        pagePanel.transform.SetAsLastSibling();
        RectTransform pr = pagePanel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;
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
        MakeBtn(topBar.transform, "Back", new Vector2(12f * s, -28f * s), new Vector2(70f * s, 36f * s),
            new Color(0.08f, 0.15f, 0.35f, 0.9f), new Color(0.75f, 0.80f, 0.95f, 1f), 12f * s,
            () => OnBackToMenu?.Invoke());

        // Title
        GameObject titleObj = new GameObject("PageTitle", typeof(RectTransform));
        titleObj.transform.SetParent(topBar.transform, false);
        TMP_Text titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "Smart QA - Buttons & Indicators";
        titleTxt.fontSize = 16f * s; titleTxt.color = new Color(0.35f, 0.70f, 1f, 1f);
        titleTxt.alignment = TextAlignmentOptions.Center; titleTxt.fontStyle = FontStyles.Bold;
        if (menuFont != null) titleTxt.font = menuFont;
        RectTransform tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = new Vector2(0, -28f * s); tr.sizeDelta = new Vector2(300f * s, 36f * s);

        // Question input (UnityEngine.UI.InputField - plain text, no TMP needed)
        GameObject inputRow = new GameObject("InputRow", typeof(RectTransform));
        inputRow.transform.SetParent(pagePanel.transform, false);
        RectTransform irr = inputRow.GetComponent<RectTransform>();
        irr.anchorMin = new Vector2(0, 1); irr.anchorMax = new Vector2(1, 1);
        irr.pivot = new Vector2(0.5f, 1);
        irr.anchoredPosition = new Vector2(0, -68f * s); irr.sizeDelta = new Vector2(-24f * s, 44f * s);

        GameObject inputBg = new GameObject("InputBg", typeof(RectTransform), typeof(Image));
        inputBg.transform.SetParent(inputRow.transform, false);
        RectTransform ibr = inputBg.GetComponent<RectTransform>();
        ibr.anchorMin = new Vector2(0, 0); ibr.anchorMax = new Vector2(1, 1);
        ibr.offsetMin = Vector2.zero; ibr.offsetMax = new Vector2(-64f * s, 0);
        inputBg.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.18f, 0.92f);

        InputField inputField = inputBg.AddComponent<InputField>();
        inputField.lineType = InputField.LineType.SingleLine;

        // Text component for input
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(inputBg.transform, false);
        Text inputText = textObj.AddComponent<Text>();
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.fontSize = 16;
        inputText.color = new Color(0.94f, 0.95f, 0.98f, 1f);
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.supportRichText = false;
        RectTransform txtr = textObj.GetComponent<RectTransform>();
        txtr.anchorMin = Vector2.zero; txtr.anchorMax = Vector2.one;
        txtr.offsetMin = new Vector2(8f * s, 0); txtr.offsetMax = new Vector2(-4f * s, 0);

        inputField.textComponent = inputText;

        // Placeholder
        GameObject phObj = new GameObject("Placeholder", typeof(RectTransform));
        phObj.transform.SetParent(inputBg.transform, false);
        Text phText = phObj.AddComponent<Text>();
        phText.text = "Ask about buttons/indicators...";
        phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        phText.fontSize = 14;
        phText.color = new Color(0.40f, 0.45f, 0.60f, 0.5f);
        phText.alignment = TextAnchor.MiddleLeft;
        phText.fontStyle = FontStyle.Italic;
        RectTransform phr = phObj.GetComponent<RectTransform>();
        phr.anchorMin = Vector2.zero; phr.anchorMax = Vector2.one;
        phr.offsetMin = new Vector2(8f * s, 0); phr.offsetMax = new Vector2(-4f * s, 0);

        inputField.placeholder = phText;

        // Ask button
        GameObject askBtn = new GameObject("AskBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        askBtn.transform.SetParent(inputRow.transform, false);
        RectTransform abr = askBtn.GetComponent<RectTransform>();
        abr.anchorMin = new Vector2(1, 0); abr.anchorMax = new Vector2(1, 1);
        abr.pivot = new Vector2(1, 0.5f); abr.anchoredPosition = Vector2.zero;
        abr.sizeDelta = new Vector2(58f * s, 0);
        askBtn.GetComponent<Image>().color = new Color(0.10f, 0.30f, 0.55f, 0.9f);
        askBtn.GetComponent<Button>().onClick.AddListener(() => smartQA?.AskQuestion());

        GameObject askLabel = new GameObject("Label", typeof(RectTransform));
        askLabel.transform.SetParent(askBtn.transform, false);
        Text askTxt = askLabel.AddComponent<Text>();
        askTxt.text = "Ask";
        askTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        askTxt.fontSize = 13;
        askTxt.color = Color.white;
        askTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform alr = askLabel.GetComponent<RectTransform>();
        alr.anchorMin = Vector2.zero; alr.anchorMax = Vector2.one; alr.sizeDelta = Vector2.zero;

        // Answer display
        GameObject answerPanel = new GameObject("AnswerPanel", typeof(RectTransform), typeof(Image));
        answerPanel.transform.SetParent(pagePanel.transform, false);
        RectTransform apr = answerPanel.GetComponent<RectTransform>();
        apr.anchorMin = new Vector2(0, 0); apr.anchorMax = new Vector2(1, 1);
        apr.offsetMin = new Vector2(8f * s, 8f * s);
        apr.offsetMax = new Vector2(-8f * s, -(120f * s));
        answerPanel.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.15f, 0.8f);

        Text answerText = answerPanel.AddComponent<Text>();
        answerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        answerText.fontSize = 13;
        answerText.color = new Color(0.85f, 0.88f, 0.93f, 1f);
        answerText.alignment = TextAnchor.UpperLeft;
        RectTransform atr = answerText.GetComponent<RectTransform>();
        atr.anchorMin = Vector2.zero; atr.anchorMax = Vector2.one;
        atr.offsetMin = new Vector2(10f * s, 10f * s);
        atr.offsetMax = new Vector2(-10f * s, -10f * s);

        // Create SmartQA component
        smartQA = gameObject.AddComponent<SmartQA>();
        smartQA.questionInput = inputField;
        smartQA.answerText = answerText;
        smartQA.askButton = askBtn.GetComponent<Button>();
    }

    void MakeBtn(Transform parent, string label, Vector2 pos, Vector2 size,
        Color bg, Color tc, float fs, System.Action action)
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
