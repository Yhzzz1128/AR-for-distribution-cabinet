using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ButtonQAPageController : MonoBehaviour
{
    public TMP_FontAsset menuFont;
    public Canvas canvas;
    public System.Action OnBackToMenu;

    private GameObject pagePanel;
    private ButtonQA buttonQA;

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

        // Semi-transparent overlay so camera feed shows through
        pagePanel = new GameObject("ButtonQAPage", typeof(RectTransform), typeof(Image));
        pagePanel.transform.SetParent(canvas.transform, false);
        pagePanel.transform.SetAsLastSibling();
        RectTransform pr = pagePanel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        pagePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);

        // Top bar - narrow, just back button and title
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(pagePanel.transform, false);
        RectTransform tb = topBar.GetComponent<RectTransform>();
        tb.anchorMin = new Vector2(0, 1); tb.anchorMax = new Vector2(1, 1);
        tb.pivot = new Vector2(0.5f, 1);
        tb.sizeDelta = new Vector2(0, 40f * s); tb.anchoredPosition = Vector2.zero;
        topBar.GetComponent<Image>().color = new Color(0.03f, 0.06f, 0.14f, 0.92f);

        // Back button (small)
        MakeBtn(topBar.transform, "<", new Vector2(8f * s, -20f * s), new Vector2(40f * s, 30f * s),
            new Color(0.08f, 0.15f, 0.35f, 0.9f), new Color(0.75f, 0.80f, 0.95f, 1f), 14f * s,
            () => OnBackToMenu?.Invoke());

        // Title
        GameObject titleObj = new GameObject("PageTitle", typeof(RectTransform));
        titleObj.transform.SetParent(topBar.transform, false);
        TMP_Text titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "Button QA";
        titleTxt.fontSize = 14f * s; titleTxt.color = new Color(0.35f, 0.70f, 1f, 1f);
        titleTxt.alignment = TextAlignmentOptions.Center; titleTxt.fontStyle = FontStyles.Bold;
        if (menuFont != null) titleTxt.font = menuFont;
        RectTransform tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = new Vector2(0, -20f * s); tr.sizeDelta = new Vector2(200f * s, 28f * s);

        // Bottom panel - contains Q&A UI
        GameObject bottomPanel = new GameObject("BottomPanel", typeof(RectTransform), typeof(Image));
        bottomPanel.transform.SetParent(pagePanel.transform, false);
        RectTransform bpr = bottomPanel.GetComponent<RectTransform>();
        bpr.anchorMin = new Vector2(0, 0); bpr.anchorMax = new Vector2(1, 0.45f);
        bpr.offsetMin = bpr.offsetMax = Vector2.zero;
        bottomPanel.GetComponent<Image>().color = new Color(0.03f, 0.06f, 0.14f, 0.94f);

        // Input row: input field + ask button
        GameObject inputRow = new GameObject("InputRow", typeof(RectTransform));
        inputRow.transform.SetParent(bottomPanel.transform, false);
        RectTransform irr = inputRow.GetComponent<RectTransform>();
        irr.anchorMin = new Vector2(0, 1); irr.anchorMax = new Vector2(1, 1);
        irr.pivot = new Vector2(0.5f, 1);
        irr.anchoredPosition = new Vector2(0, -8f * s); irr.sizeDelta = new Vector2(-16f * s, 38f * s);

        // Input field
        GameObject inputBg = new GameObject("InputBg", typeof(RectTransform), typeof(Image));
        inputBg.transform.SetParent(inputRow.transform, false);
        RectTransform ibr = inputBg.GetComponent<RectTransform>();
        ibr.anchorMin = new Vector2(0, 0); ibr.anchorMax = new Vector2(1, 1);
        ibr.offsetMin = Vector2.zero; ibr.offsetMax = new Vector2(-52f * s, 0);
        inputBg.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.18f, 0.92f);

        InputField inputField = inputBg.AddComponent<InputField>();
        inputField.lineType = InputField.LineType.SingleLine;

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(inputBg.transform, false);
        Text inputText = textObj.AddComponent<Text>();
        inputText.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        inputText.fontSize = 14;
        inputText.color = new Color(0.94f, 0.95f, 0.98f, 1f);
        inputText.alignment = TextAnchor.MiddleLeft;
        RectTransform txtr = textObj.GetComponent<RectTransform>();
        txtr.anchorMin = Vector2.zero; txtr.anchorMax = Vector2.one;
        txtr.offsetMin = new Vector2(6f * s, 0); txtr.offsetMax = new Vector2(-4f * s, 0);
        inputField.textComponent = inputText;

        // Placeholder
        GameObject phObj = new GameObject("Placeholder", typeof(RectTransform));
        phObj.transform.SetParent(inputBg.transform, false);
        Text phText = phObj.AddComponent<Text>();
        phText.text = "Ask about buttons...";
        phText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        phText.fontSize = 12;
        phText.color = new Color(0.40f, 0.45f, 0.60f, 0.5f);
        phText.alignment = TextAnchor.MiddleLeft;
        RectTransform phr = phObj.GetComponent<RectTransform>();
        phr.anchorMin = Vector2.zero; phr.anchorMax = Vector2.one;
        phr.offsetMin = new Vector2(6f * s, 0); phr.offsetMax = new Vector2(-4f * s, 0);
        inputField.placeholder = phText;

        // Ask button (compact, right side of input)
        GameObject askBtn = new GameObject("AskBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        askBtn.transform.SetParent(inputRow.transform, false);
        RectTransform abr = askBtn.GetComponent<RectTransform>();
        abr.anchorMin = new Vector2(1, 0); abr.anchorMax = new Vector2(1, 1);
        abr.pivot = new Vector2(1, 0.5f); abr.anchoredPosition = Vector2.zero;
        abr.sizeDelta = new Vector2(46f * s, 0);
        askBtn.GetComponent<Image>().color = new Color(0.10f, 0.30f, 0.55f, 0.9f);

        GameObject askLabel = new GameObject("Label", typeof(RectTransform));
        askLabel.transform.SetParent(askBtn.transform, false);
        Text askTxt = askLabel.AddComponent<Text>();
        askTxt.text = "Ask";
        askTxt.font = Font.CreateDynamicFontFromOSFont("Arial", 13);
        askTxt.fontSize = 11;
        askTxt.color = Color.white;
        askTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform alr = askLabel.GetComponent<RectTransform>();
        alr.anchorMin = Vector2.zero; alr.anchorMax = Vector2.one; alr.sizeDelta = Vector2.zero;

        // Answer display
        GameObject answerPanel = new GameObject("AnswerPanel", typeof(RectTransform), typeof(Image));
        answerPanel.transform.SetParent(bottomPanel.transform, false);
        RectTransform apr = answerPanel.GetComponent<RectTransform>();
        apr.anchorMin = new Vector2(0, 0); apr.anchorMax = new Vector2(1, 1);
        apr.offsetMin = new Vector2(6f * s, 6f * s);
        apr.offsetMax = new Vector2(-6f * s, -(50f * s));
        answerPanel.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.15f, 0.7f);

        Text answerText = answerPanel.AddComponent<Text>();
        answerText.font = Font.CreateDynamicFontFromOSFont("Arial", 13);
        answerText.fontSize = 12;
        answerText.color = new Color(0.85f, 0.88f, 0.93f, 1f);
        answerText.alignment = TextAnchor.UpperLeft;
        RectTransform atr = answerText.GetComponent<RectTransform>();
        atr.anchorMin = Vector2.zero; atr.anchorMax = Vector2.one;
        atr.offsetMin = new Vector2(8f * s, 8f * s);
        atr.offsetMax = new Vector2(-8f * s, -8f * s);

        // Create ButtonQA and wire up
        buttonQA = gameObject.AddComponent<ButtonQA>();
        buttonQA.questionInput = inputField;
        buttonQA.answerText = answerText;
        buttonQA.askButton = askBtn.GetComponent<Button>();

        // Also trigger on Enter key
        inputField.onEndEdit.AddListener((val) => {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                buttonQA.AskQuestion();
        });
    }

    void MakeBtn(Transform parent, string label, Vector2 pos, Vector2 size,
        Color bg, Color tc, float fs, System.Action action)
    {
        GameObject btn = new GameObject("Btn_", typeof(RectTransform), typeof(Image), typeof(Button));
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
