using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class FaultChallengePageController : MonoBehaviour
{
    public TMP_FontAsset menuFont;
    public Canvas canvas;
    public Action OnBackToMenu;

    private GameObject pagePanel;
    private TMP_Text statusText;
    private TMP_Text hintText;
    private FaultChallengeManager challengeManager;

    public void ConfigureChallengeManager(FaultChallengeManager manager)
    {
        challengeManager = manager;
        UpdateHint();
    }

    public void Show()
    {
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[FaultChallengePage] Canvas not found");
            return;
        }

        if (pagePanel == null)
        {
            CreatePage();
        }

        pagePanel.SetActive(true);
        UpdateStatus("请选择一个故障挑战并按照提示在 AR 场景中操作。");
        UpdateHint();
    }

    public void Hide()
    {
        if (pagePanel != null)
        {
            pagePanel.SetActive(false);
        }
    }

    void CreatePage()
    {
        float s = Mathf.Clamp(Screen.width / 540f, 1f, 2f);

        pagePanel = new GameObject("FaultChallengePage", typeof(RectTransform), typeof(Image));
        pagePanel.transform.SetParent(canvas.transform, false);
        pagePanel.transform.SetAsLastSibling();
        RectTransform pr = pagePanel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;
        pagePanel.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.12f, 1f);

        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(pagePanel.transform, false);
        RectTransform tb = topBar.GetComponent<RectTransform>();
        tb.anchorMin = new Vector2(0, 1);
        tb.anchorMax = new Vector2(1, 1);
        tb.pivot = new Vector2(0.5f, 1f);
        tb.anchoredPosition = Vector2.zero;
        tb.sizeDelta = new Vector2(0, 60f * s);
        topBar.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.18f, 0.95f);

        CreateButton(topBar.transform, "Back", new Vector2(14f * s, -30f * s), new Vector2(78f * s, 36f * s),
            new Color(0.08f, 0.15f, 0.35f, 0.9f), new Color(0.75f, 0.80f, 0.95f, 1f), 14f * s,
            () => { challengeManager?.ExitChallenge(); OnBackToMenu?.Invoke(); });

        TMP_Text title = CreateText(topBar.transform, "故障挑战", 20f * s, new Color(0.35f, 0.70f, 1f, 1f));
        RectTransform tr = title.rectTransform;
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = new Vector2(0, -30f * s);
        tr.sizeDelta = new Vector2(220f * s, 36f * s);
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;
        ApplyFont(title);

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(pagePanel.transform, false);
        RectTransform cr = content.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 1f);
        cr.anchorMax = new Vector2(0.5f, 1f);
        cr.pivot = new Vector2(0.5f, 1f);
        cr.anchoredPosition = new Vector2(0, -92f * s);
        cr.sizeDelta = new Vector2(320f * s, 480f * s);

        statusText = CreateText(content.transform, string.Empty, 12f * s, new Color(0.45f, 0.50f, 0.62f, 1f));
        RectTransform sr = statusText.rectTransform;
        sr.anchorMin = new Vector2(0.5f, 1f);
        sr.anchorMax = new Vector2(0.5f, 1f);
        sr.pivot = new Vector2(0.5f, 1f);
        sr.anchoredPosition = new Vector2(0, 0);
        sr.sizeDelta = new Vector2(320f * s, 48f * s);
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.enableWordWrapping = true;
        ApplyFont(statusText);

        hintText = CreateText(content.transform, string.Empty, 11f * s, new Color(0.55f, 0.60f, 0.75f, 0.9f));
        RectTransform hr = hintText.rectTransform;
        hr.anchorMin = new Vector2(0.5f, 1f);
        hr.anchorMax = new Vector2(0.5f, 1f);
        hr.pivot = new Vector2(0.5f, 1f);
        hr.anchoredPosition = new Vector2(0, -58f * s);
        hr.sizeDelta = new Vector2(320f * s, 60f * s);
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.enableWordWrapping = true;
        ApplyFont(hintText);

        float buttonHeight = 48f * s;
        float buttonWidth = 260f * s;
        float startY = -140f * s;
        float step = -66f * s;

        CreateButton(content.transform, "指示灯不亮挑战", new Vector2(0, startY), new Vector2(buttonWidth, buttonHeight),
            new Color(0.18f, 0.32f, 0.58f, 0.95f), new Color(0.93f, 0.97f, 1f, 1f), 15f * s,
            () => StartChallenge(FaultType.Indicator));

        CreateButton(content.transform, "断路器跳闸挑战", new Vector2(0, startY + step), new Vector2(buttonWidth, buttonHeight),
            new Color(0.22f, 0.36f, 0.62f, 0.95f), new Color(0.93f, 0.97f, 1f, 1f), 15f * s,
            () => StartChallenge(FaultType.Breaker));

        CreateButton(content.transform, "柜内异味挑战", new Vector2(0, startY + step * 2f), new Vector2(buttonWidth, buttonHeight),
            new Color(0.26f, 0.40f, 0.68f, 0.95f), new Color(0.93f, 0.97f, 1f, 1f), 15f * s,
            () => StartChallenge(FaultType.Smell));

        CreateButton(content.transform, "退出挑战模式", new Vector2(0, startY + step * 3f), new Vector2(buttonWidth, buttonHeight),
            new Color(0.40f, 0.18f, 0.18f, 0.95f), new Color(1f, 0.95f, 0.92f, 1f), 14f * s,
            ExitChallenge);
    }

    void StartChallenge(FaultType type)
    {
        if (challengeManager == null)
        {
            UpdateStatus("未找到 FaultChallengeManager，无法启动挑战。");
            return;
        }

        switch (type)
        {
            case FaultType.Indicator:
                challengeManager.StartIndicatorFault();
                UpdateStatus("已启动：指示灯不亮挑战。");
                break;
            case FaultType.Breaker:
                challengeManager.StartBreakerFault();
                UpdateStatus("已启动：断路器跳闸挑战。");
                break;
            case FaultType.Smell:
                challengeManager.StartSmellFault();
                UpdateStatus("已启动：柜内异味挑战。");
                break;
        }

        UpdateHint();
    }

    void ExitChallenge()
    {
        if (challengeManager != null)
        {
            challengeManager.ExitChallenge();
        }
        UpdateStatus("已退出挑战模式，返回 AR 可继续普通模式。");
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    void UpdateHint()
    {
        if (hintText == null) return;

        if (challengeManager != null && challengeManager.challengeMode)
        {
            hintText.text = "挑战进行中：请在 AR 场景中点击对应部件完成诊断。";
        }
        else
        {
            hintText.text = "点击下方任一挑战按钮后，再切换至 AR 检测界面进行操作。";
        }
    }

    TMP_Text CreateText(Transform parent, string text, float fontSize, Color color)
    {
        GameObject go = new GameObject("Text" + parent.childCount, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        ApplyFont(tmp);
        return tmp;
    }

    void CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, Color bg, Color tc, float fs, Action onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform br = btnObj.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0.5f, 1f);
        br.anchorMax = new Vector2(0.5f, 1f);
        br.pivot = new Vector2(0.5f, 0.5f);
        br.anchoredPosition = pos;
        br.sizeDelta = size;
        btnObj.GetComponent<Image>().color = bg;
        btnObj.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());

        GameObject lbl = new GameObject("Label", typeof(RectTransform));
        lbl.transform.SetParent(btnObj.transform, false);
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;

        TMP_Text txt = lbl.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = fs;
        txt.color = tc;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold;
        ApplyFont(txt);
    }

    void ApplyFont(TMP_Text txt)
    {
        if (menuFont != null)
        {
            txt.font = menuFont;
        }
    }

    enum FaultType
    {
        Indicator,
        Breaker,
        Smell
    }
}
