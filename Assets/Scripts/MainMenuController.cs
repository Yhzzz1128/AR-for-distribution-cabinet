using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    [Header("Font")]
    public TMP_FontAsset menuFont;

    [Header("Splash")]
    public float splashDuration = 2.5f;
    public string appTitle = "PDG AR System";
    public string appSubtitle = "Power Distribution Cabinet · Inspection & Guide";
    public Color splashBgColor = new Color(0.02f, 0.04f, 0.10f, 1f);
    public Color accentColor = new Color(0.35f, 0.70f, 1f, 1f);

    [Header("Main Menu")]
    public string menuTitle = "PDG AR System";
    public string menuDesc = "Smart power distribution cabinet assistant\nAR inspection · fault diagnosis · knowledge base";
    public string btnARText = "AR Inspection";
    public string btnQAText = "Knowledge Base";
    public Color menuBgColor = new Color(0.03f, 0.06f, 0.14f, 0.96f);
    public Color buttonColor = new Color(0.10f, 0.30f, 0.55f, 0.90f);
    public Color buttonTextColor = new Color(0.90f, 0.93f, 0.98f, 1f);

    private Canvas canvas;
    private GameObject splashPanel;
    private GameObject menuPanel;
    private QAPageController qaPage;
    private bool menuShown = false;

    void Awake()
    {
        // Auto-load Chinese font from AI_Search_Manager
        if (menuFont == null)
        {
            var searchMgr = FindObjectOfType<AI_Search_Manager>();
            if (searchMgr != null && searchMgr.chineseFont != null)
                menuFont = searchMgr.chineseFont;
        }
    }

    void Start()
    {
        canvas = FindObjectOfType<Canvas>();
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

        DisableARExperience();
        CreateSplashPanel();
        StartCoroutine(SplashSequence());
    }

    void DisableARExperience()
    {
        var interactor = FindObjectOfType<ARInteractor>();
        if (interactor != null) interactor.enabled = false;

        // Keep AI_Search_Manager enabled so it initializes properly;
        // just hide its UI elements until AR mode is entered
        var qaPanel = GameObject.Find("QAPanel");
        if (qaPanel != null) qaPanel.SetActive(false);

        var sousuo = GameObject.Find("sousuo");
        if (sousuo != null) sousuo.SetActive(false);

        var resultPanel = GameObject.Find("ResultPanel");
        if (resultPanel != null) resultPanel.SetActive(false);

        if (arBackButton != null) arBackButton.SetActive(false);
    }

    private GameObject arBackButton;

    void EnableARExperience()
    {
        var interactor = FindObjectOfType<ARInteractor>();
        if (interactor != null) interactor.enabled = true;

        var searchMgr = FindObjectOfType<AI_Search_Manager>();
        if (searchMgr != null)
        {
            searchMgr.enabled = true;
            // Force re-init if needed (handles case where Start was deferred)
            if (!searchMgr.gameObject.activeInHierarchy)
                searchMgr.gameObject.SetActive(true);
        }

        var sousuo = GameObject.Find("sousuo");
        if (sousuo != null) sousuo.SetActive(true);

        var resultPanel = GameObject.Find("ResultPanel");
        if (resultPanel != null) resultPanel.SetActive(true);

        // Create back button on AR page
        if (arBackButton == null)
        {
            float s = GetScale();
            arBackButton = new GameObject("ARBackBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            arBackButton.transform.SetParent(canvas.transform, false);
            arBackButton.transform.SetAsLastSibling();
            RectTransform br = arBackButton.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(0, 0.5f);
            float topY = -30f * s;
            br.anchoredPosition = new Vector2(12f * s, Screen.height - Mathf.Abs(topY));
            br.anchoredPosition = new Vector2(12f * s, -30f * s); // top-left corner area
            br.sizeDelta = new Vector2(70f * s, 36f * s);
            arBackButton.GetComponent<Image>().color = new Color(0.08f, 0.15f, 0.35f, 0.9f);
            arBackButton.GetComponent<Button>().onClick.AddListener(ReturnToMenu);

            GameObject lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(arBackButton.transform, false);
            TMP_Text txt = lbl.AddComponent<TextMeshProUGUI>();
            txt.text = "Back";
            txt.fontSize = 12f * s;
            txt.color = new Color(0.75f, 0.80f, 0.95f, 1f);
            txt.alignment = TextAlignmentOptions.Center;
            if (menuFont != null) txt.font = menuFont;
            RectTransform lr = lbl.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.sizeDelta = Vector2.zero;
        }
        arBackButton.SetActive(true);
    }

    // ===== Splash =====
    void CreateSplashPanel()
    {
        float s = GetScale();
        splashPanel = MakeFullscreenPanel("SplashPanel", splashBgColor);

        MakeTMPText("SplashTitle", splashPanel.transform, appTitle, 28f * s, accentColor, FontStyles.Bold,
            new Vector2(0, 20f * s), new Vector2(320f * s, 50f * s));

        MakeTMPText("SplashSub", splashPanel.transform, appSubtitle, 13f * s, new Color(0.50f, 0.58f, 0.72f, 1f), FontStyles.Normal,
            new Vector2(0, -24f * s), new Vector2(300f * s, 30f * s));

        GameObject loadObj = MakeTMPText("LoadingDots", splashPanel.transform, "...", 20f * s,
            new Color(0.60f, 0.65f, 0.80f, 0.6f), FontStyles.Normal,
            new Vector2(0, -60f * s), new Vector2(100f * s, 30f * s));
        StartCoroutine(PulseDots(loadObj.GetComponent<TMP_Text>()));
    }

    IEnumerator PulseDots(TMP_Text txt)
    {
        float t = 0f;
        while (splashPanel != null && splashPanel.activeSelf)
        {
            t += Time.deltaTime * 2f;
            txt.color = new Color(0.60f, 0.65f, 0.80f, 0.3f + Mathf.Sin(t) * 0.4f);
            yield return null;
        }
    }

    IEnumerator SplashSequence()
    {
        CanvasGroup cg = splashPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        for (float e = 0f; e < 0.6f; e += Time.deltaTime) { cg.alpha = e / 0.6f; yield return null; }
        cg.alpha = 1f;
        yield return new WaitForSeconds(splashDuration);
        for (float e = 0f; e < 0.5f; e += Time.deltaTime) { cg.alpha = 1f - e / 0.5f; yield return null; }
        splashPanel.SetActive(false);
        yield return StartCoroutine(ShowMainMenu());
    }

    // ===== Main Menu =====
    IEnumerator ShowMainMenu()
    {
        if (menuShown) yield break;
        menuShown = true;
        CreateMenuPanel();

        CanvasGroup cg = menuPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        for (float e = 0f; e < 0.5f; e += Time.deltaTime) { cg.alpha = e / 0.5f; yield return null; }
        cg.alpha = 1f;
    }

    void CreateMenuPanel()
    {
        float s = GetScale();
        menuPanel = MakeFullscreenPanel("MainMenuPanel", menuBgColor);

        GameObject content = new GameObject("MenuContent", typeof(RectTransform));
        content.transform.SetParent(menuPanel.transform, false);
        RectTransform cr = content.GetComponent<RectTransform>();
        SetAnchorCenter(cr, Vector2.zero, new Vector2(340f * s, 420f * s));

        MakeTMPText("MenuTitle", content.transform, menuTitle, 26f * s, accentColor, FontStyles.Bold,
            new Vector2(0, -10f * s), new Vector2(320f * s, 44f * s), TextAlignmentOptions.Center);

        // Divider
        GameObject div = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        div.transform.SetParent(content.transform, false);
        RectTransform dr = div.GetComponent<RectTransform>();
        SetAnchorTop(dr, new Vector2(0, -58f * s), new Vector2(220f * s, 1f));
        div.GetComponent<Image>().color = new Color(0.15f, 0.30f, 0.50f, 0.6f);

        MakeTMPText("MenuDesc", content.transform, menuDesc, 12f * s, new Color(0.55f, 0.60f, 0.72f, 1f), FontStyles.Normal,
            new Vector2(0, -74f * s), new Vector2(300f * s, 60f * s), TextAlignmentOptions.Center);

        // Button 1: AR Inspection
        MakeMenuButton(content.transform, btnARText, 0, -150f * s, 240f * s, 52f * s, s, () => StartCoroutine(EnterARMode()));

        // Button 2: Knowledge Base
        MakeMenuButton(content.transform, btnQAText, 0, -220f * s, 240f * s, 52f * s, s, () => StartCoroutine(EnterQAMode()));
    }

    void MakeMenuButton(Transform parent, string label, float x, float y, float w, float h, float s, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject("Btn_" + label.Replace(" ", ""), typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform br = btnObj.GetComponent<RectTransform>();
        SetAnchorTop(br, new Vector2(x, y), new Vector2(w, h));
        btnObj.GetComponent<Image>().color = buttonColor;
        btnObj.GetComponent<Button>().onClick.AddListener(action);

        GameObject lbl = new GameObject("Label", typeof(RectTransform));
        lbl.transform.SetParent(btnObj.transform, false);
        TMP_Text txt = lbl.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 16f * s;
        txt.color = buttonTextColor;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold;
        if (menuFont != null) txt.font = menuFont;
        RectTransform lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.sizeDelta = Vector2.zero;
    }

    // ===== Transitions =====
    IEnumerator EnterARMode()
    {
        yield return FadeOutPanel(menuPanel);
        menuPanel.SetActive(false);
        EnableARExperience();
    }

    IEnumerator EnterQAMode()
    {
        yield return FadeOutPanel(menuPanel);
        menuPanel.SetActive(false);

        if (qaPage == null)
        {
            qaPage = gameObject.AddComponent<QAPageController>();
            qaPage.menuFont = menuFont;
            qaPage.canvas = canvas;
            qaPage.OnBackToMenu += OnBackFromQAPage;
        }
        qaPage.Show();
    }

    void OnBackFromQAPage()
    {
        if (qaPage != null) qaPage.Hide();
        menuPanel.SetActive(true);
        CanvasGroup cg = menuPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = menuPanel.AddComponent<CanvasGroup>();
        StartCoroutine(FadeInPanel(menuPanel, cg));
    }

    public void ReturnToMenu()
    {
        // Called by back button on AR page
        DisableARExperience();
        menuPanel.SetActive(true);
        CanvasGroup cg = menuPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = menuPanel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
    }

    IEnumerator FadeOutPanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        for (float e = 0f; e < 0.35f; e += Time.deltaTime) { cg.alpha = 1f - e / 0.35f; yield return null; }
        cg.alpha = 0f;
    }

    IEnumerator FadeInPanel(GameObject panel, CanvasGroup cg)
    {
        cg.alpha = 0f;
        for (float e = 0f; e < 0.35f; e += Time.deltaTime) { cg.alpha = e / 0.35f; yield return null; }
        cg.alpha = 1f;
    }

    // ===== Helpers =====
    float GetScale() { return Mathf.Clamp(Screen.width / 540f, 1f, 2f); }

    GameObject MakeFullscreenPanel(string name, Color bgColor)
    {
        GameObject p = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        p.transform.SetParent(canvas.transform, false);
        p.transform.SetAsLastSibling();
        RectTransform r = p.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        p.GetComponent<Image>().color = bgColor;
        return p;
    }

    GameObject MakeTMPText(string name, Transform parent, string text, float fontSize, Color color, FontStyles style,
        Vector2 pos, Vector2 size, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TMP_Text txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = align;
        txt.fontStyle = style;
        if (menuFont != null) txt.font = menuFont;
        RectTransform r = go.GetComponent<RectTransform>();
        SetAnchorCenter(r, pos, size);
        return go;
    }

    void SetAnchorCenter(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    void SetAnchorTop(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    void SetAnchorTopLeft(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    void SetStretch(RectTransform r, Vector2 offsetMin, Vector2 offsetMax)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = offsetMin; r.offsetMax = offsetMax;
    }

    // Auto-init
    public static class MainMenuAutoInit
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if (Object.FindObjectOfType<MainMenuController>() != null) return;
            GameObject go = new GameObject("[Auto] MainMenuController", typeof(MainMenuController));
            Object.DontDestroyOnLoad(go);
        }
    }
}


