using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AI_Search_Manager : MonoBehaviour
{
    [Header("AI 在线问答配置")]
    public string apiKey = "sk-your-deepseek-api-key";
    public string apiUrl = "https://api.deepseek.com/chat/completions";
    public string model = "deepseek-v4-flash";
    [Tooltip("启用在线 AI 问答（需要有效的 API Key）。\n注意：API Key 会打包到 APK 中，有泄露风险。\n建议在生产环境下通过后端代理转发。")]
    public bool useOnlineAI = false;
    public float requestTimeoutSeconds = 20f;

    [Header("UI 引用")]
    public TMP_InputField inputField;
    public Transform contentPanel;
    public GameObject itemPrefab;
    public TMP_FontAsset chineseFont;

    [Header("问答设置")]
    public bool askOnValueChanged = false;
    public float debounceSeconds = 0.8f;
    [TextArea(2, 5)]
    public string emptyHint = "请输入配电柜相关问题，例如：停电前要检查什么？熔断器故障怎么处理？";

    private const string PlaceholderApiKey = "sk-your-deepseek-api-key";
    private readonly List<OperationEntry> knowledgeEntries = new List<OperationEntry>();
    private string loadedJsonData = "";
    private Coroutine pendingAskCoroutine;
    private UnityWebRequest activeRequest;
    private string lastSubmittedQuestion = "";
    private float lastSubmittedTime;
    private RectTransform resultContent;
    private ScrollRect resultScrollRect;

    // Step-by-step state
    private List<string> pendingAnswerLines = new List<string>();
    private int currentStepIndex = 0;
    private GameObject nextStepButton;
    private GameObject prevStepButton;
    private GameObject currentStepObj;
    private List<GameObject> persistentSafetyItems = new List<GameObject>();
    // Multi-result selection state
    private List<OperationEntry> selectionMatches;
    private bool isShowingSelection = false;


    private void Start()
    {
        ResolveSceneReferences();
        LoadLocalKnowledgeBase();
        BindInputEvents();
        ConfigureInputField();
        ConfigureResultPanel();
        ShowHint();
    }

    private void Update()
    {
        if (inputField == null)
        {
            return;
        }
        HandleResultMouseWheel();

        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return;
        }

        bool inputIsActive = inputField.isFocused ||
                             EventSystem.current != null &&
                             EventSystem.current.currentSelectedGameObject == inputField.gameObject;

        if (inputIsActive || !string.IsNullOrWhiteSpace(inputField.text))
        {
            SubmitCurrentQuestion();
        }
    }

    private void OnDestroy()
    {
        if (activeRequest != null)
        {
            activeRequest.Abort();
            activeRequest.Dispose();
            activeRequest = null;
        }
    }

    private void BindInputEvents()
    {
        if (inputField == null)
        {
            Debug.LogError("AI 问答未绑定输入框 inputField。");
            return;
        }

        inputField.onSubmit.AddListener(SubmitQuestion);
        inputField.onEndEdit.AddListener(SubmitQuestion);
        inputField.onValueChanged.AddListener(OnInputChanged);
    }

    private void ResolveSceneReferences()
    {
        if (inputField == null)
        {
            GameObject inputObject = GameObject.Find("sousuo");
            if (inputObject != null)
            {
                inputField = inputObject.GetComponent<TMP_InputField>();
            }
        }

        if (inputField == null)
        {
            foreach (TMP_InputField field in Resources.FindObjectsOfTypeAll<TMP_InputField>())
            {
                if (field.name == "sousuo" && field.gameObject.scene.IsValid())
                {
                    inputField = field;
                    break;
                }
            }
        }

        if (contentPanel == null)
        {
            GameObject resultObject = GameObject.Find("ResultPanel");
            if (resultObject != null)
            {
                contentPanel = resultObject.transform;
            }
        }

        if (contentPanel == null)
        {
            foreach (RectTransform rect in Resources.FindObjectsOfTypeAll<RectTransform>())
            {
                if (rect.name == "ResultPanel" && rect.gameObject.scene.IsValid())
                {
                    contentPanel = rect;
                    break;
                }
            }
        }
    }

    private void ConfigureInputField()
    {
        if (inputField == null)
        {
            return;
        }

        inputField.interactable = true;
        inputField.readOnly = false;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.shouldHideMobileInput = Application.isMobilePlatform;
        inputField.resetOnDeActivation = false;

        PositionInputFieldForScreen();
        // Tech-style input field background
        Image inputBg = inputField.GetComponent<Image>();
        if (inputBg != null)
        {
            inputBg.color = new Color(0.06f, 0.09f, 0.18f, 0.92f);
        }
        Outline inputOutline = inputField.GetComponent<Outline>();
        if (inputOutline == null)
        {
            inputOutline = inputField.gameObject.AddComponent<Outline>();
        }
        inputOutline.effectColor = new Color(0.18f, 0.55f, 0.92f, 0.3f);
        inputOutline.effectDistance = new Vector2(1f, -1f);
        inputOutline.useGraphicAlpha = false;

        float uiScale = GetAiUiScale();
        if (inputField.textComponent != null)
        {
            inputField.textComponent.enabled = true;
            inputField.textComponent.gameObject.SetActive(true);
            inputField.textComponent.color = new Color(0.94f, 0.95f, 0.98f, 1f);
            inputField.textComponent.fontSize = 16f * uiScale;
            inputField.textComponent.enableWordWrapping = false;

            if (chineseFont != null)
            {
                inputField.textComponent.font = chineseFont;
            }

            RectTransform textRect = inputField.textComponent.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f * uiScale, 0);
            textRect.offsetMax = new Vector2(-4f * uiScale, 0);
        }

        if (inputField.placeholder is TMP_Text placeholder)
        {
            placeholder.enabled = true;
            placeholder.color = new Color(0.50f, 0.55f, 0.68f, 0.55f);
            placeholder.fontSize = 16f * uiScale;

            if (chineseFont != null)
            {
                placeholder.font = chineseFont;
            }
        }

        inputField.text = inputField.text ?? "";
        inputField.ForceLabelUpdate();
        Canvas.ForceUpdateCanvases();
        StartCoroutine(RebuildInputFieldNextFrame());
    }

    private void EnsureRuntimeInputText()
    {
        // Use native text component instead of replacing it (replacement breaks caret)
        if (inputField == null || inputField.textComponent == null) return;
        float uiScale = GetAiUiScale();
        inputField.textComponent.enabled = true;
        inputField.textComponent.raycastTarget = false;
        inputField.textComponent.color = new Color(0.94f, 0.95f, 0.98f, 1f);
        inputField.textComponent.fontSize = 16f * uiScale;
        inputField.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
        inputField.textComponent.enableWordWrapping = false;
        inputField.textComponent.overflowMode = TextOverflowModes.Overflow;
        if (chineseFont != null) inputField.textComponent.font = chineseFont;
    }



    private IEnumerator RebuildInputFieldNextFrame()
    {
        yield return null;

        if (inputField == null)
        {
            yield break;
        }

        if (inputField.textComponent != null)
        {
            inputField.textComponent.ForceMeshUpdate(true, true);
        }
        inputField.ForceLabelUpdate();
        Canvas.ForceUpdateCanvases();
    }

    private void ConfigureResultPanel()
    {
        if (!EnsureContentPanelAvailable())
        {
            return;
        }

        PositionResultPanelInGameView();

        VerticalLayoutGroup rootLayout = contentPanel.GetComponent<VerticalLayoutGroup>();
        if (rootLayout != null)
        {
            rootLayout.enabled = false;
        }

        ContentSizeFitter fitter = contentPanel.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.enabled = false;
        }

        RectMask2D mask = contentPanel.GetComponent<RectMask2D>();
        if (mask != null)
        {
            Destroy(mask);
        }

        EnsureResultScrollView();
    }

    private void EnsureResultScrollView()
    {
        if (!(contentPanel is RectTransform panelRect))
        {
            return;
        }

        float uiScale = GetAiUiScale();
        Transform viewportTransform = contentPanel.Find("AIResultViewport");
        GameObject viewportObject = viewportTransform != null
            ? viewportTransform.gameObject
            : new GameObject("AIResultViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));

        viewportObject.transform.SetParent(contentPanel, false);
        viewportObject.transform.SetAsLastSibling();

        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f * uiScale, 10f * uiScale);
        viewportRect.offsetMax = new Vector2(-10f * uiScale, -10f * uiScale);
        viewportRect.pivot = new Vector2(0.5f, 0.5f);

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0f);
        viewportImage.raycastTarget = true;

        if (viewportObject.GetComponent<RectMask2D>() == null)
        {
            viewportObject.AddComponent<RectMask2D>();
        }

        Transform contentTransform = viewportObject.transform.Find("AIResultContent");
        GameObject contentObject = contentTransform != null
            ? contentTransform.gameObject
            : new GameObject("AIResultContent", typeof(RectTransform));

        contentObject.transform.SetParent(viewportObject.transform, false);
        resultContent = contentObject.GetComponent<RectTransform>();
        resultContent.anchorMin = new Vector2(0f, 1f);
        resultContent.anchorMax = new Vector2(1f, 1f);
        resultContent.pivot = new Vector2(0.5f, 1f);
        resultContent.anchoredPosition = Vector2.zero;
        resultContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        if (contentLayout == null)
        {
            contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
        }

        int padding = Mathf.RoundToInt(2f * uiScale);
        contentLayout.padding = new RectOffset(padding, padding, padding, padding);
        contentLayout.spacing = 4f * uiScale;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
        if (contentFitter == null)
        {
            contentFitter = contentObject.AddComponent<ContentSizeFitter>();
        }

        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        resultScrollRect = contentPanel.GetComponent<ScrollRect>();
        if (resultScrollRect == null)
        {
            resultScrollRect = contentPanel.gameObject.AddComponent<ScrollRect>();
        }

        resultScrollRect.viewport = viewportRect;
        resultScrollRect.content = resultContent;
        resultScrollRect.horizontal = false;
        resultScrollRect.vertical = true;
        resultScrollRect.enabled = true;
        resultScrollRect.movementType = ScrollRect.MovementType.Clamped;
        resultScrollRect.inertia = true;
        resultScrollRect.scrollSensitivity = 24f * uiScale;

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
    }

    private void HandleResultMouseWheel()
    {
        if (resultScrollRect == null || resultContent == null || !(contentPanel is RectTransform panelRect))
        {
            return;
        }

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) < 0.01f)
        {
            return;
        }

        Canvas canvas = ResolveUICanvas();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition, eventCamera))
        {
            return;
        }

        resultScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            resultScrollRect.verticalNormalizedPosition + wheel * 0.12f);
    }

    private bool EnsureContentPanelAvailable()
    {
        if (contentPanel != null)
        {
            return true;
        }

        ResolveSceneReferences();
        if (contentPanel != null)
        {
            return true;
        }

        Transform parent = inputField != null && inputField.transform.parent != null
            ? inputField.transform.parent
            : null;

        if (parent == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            parent = canvas != null ? canvas.transform : transform;
        }

        GameObject panel = new GameObject("RuntimeAIResultPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.transform.SetAsLastSibling();

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = GetResultPanelSize();
        rect.anchoredPosition = GetResultPanelPosition();

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.04f, 0.06f, 0.12f, 0.94f);
        image.raycastTarget = true;

        contentPanel = panel.transform;
        return true;
    }

    private void PositionResultPanelInGameView()
    {
        if (!(contentPanel is RectTransform rect))
        {
            return;
        }

        Canvas canvas = ResolveUICanvas();
        if (canvas != null && rect.parent != canvas.transform)
        {
            rect.SetParent(canvas.transform, false);
        }

        Vector2 resultSize = GetResultPanelSize();
        float panelWidth = resultSize.x;
        float panelHeight = resultSize.y;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = GetResultPanelPosition();
        rect.sizeDelta = new Vector2(panelWidth, panelHeight);
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();

        Image image = contentPanel.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.04f, 0.06f, 0.12f, 0.94f);
            image.raycastTarget = true;
        }
    }

    private void PositionInputFieldForScreen()
    {
        if (inputField == null || !(inputField.transform is RectTransform rect))
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = GetInputFieldPosition();
        rect.sizeDelta = GetInputFieldSize();
        rect.localScale = Vector3.one;
    }

    private Vector2 GetInputFieldPosition()
    {
        float uiScale = GetAiUiScale();
        return new Vector2(24f * uiScale, -56f * uiScale);
    }

    private Vector2 GetInputFieldSize()
    {
        float uiScale = GetAiUiScale();
        return new Vector2(
            Mathf.Clamp(210f * uiScale, 210f, 460f),
            Mathf.Clamp(44f * uiScale, 44f, 96f));
    }

    private Vector2 GetResultPanelPosition()
    {
        Vector2 inputPosition = GetInputFieldPosition();
        Vector2 inputSize = GetInputFieldSize();
        float uiScale = GetAiUiScale();
        return new Vector2(inputPosition.x, inputPosition.y - inputSize.y - 18f * uiScale);
    }

    private Vector2 GetResultPanelSize()
    {
        float uiScale = GetAiUiScale();
        return new Vector2(
            Mathf.Clamp(300f * uiScale, 300f, 560f),
            Mathf.Clamp(180f * uiScale, 180f, 400f));
    }

    private float GetAiUiScale()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return 1f;
        }

        float scale = Mathf.Min(Screen.width / 540f, Screen.height / 960f);
        return Mathf.Clamp(scale, 1f, 2f);
    }

    private Canvas ResolveUICanvas()
    {
        if (contentPanel != null)
        {
            Canvas canvas = contentPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }
        }

        if (inputField != null)
        {
            Canvas canvas = inputField.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }
        }

        return FindObjectOfType<Canvas>();
    }

    private void LoadLocalKnowledgeBase()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("OperationData");

        if (jsonFile == null)
        {
            Debug.LogWarning("未找到 Resources/OperationData.json，AI 问答将只能依赖在线模型。");
            return;
        }

        loadedJsonData = jsonFile.text;
        knowledgeEntries.Clear();

        try
        {
            OperationDataWrapper wrapper = JsonUtility.FromJson<OperationDataWrapper>(WrapJsonArray(loadedJsonData));
            if (wrapper != null && wrapper.items != null)
            {
                knowledgeEntries.AddRange(wrapper.items);
            }

            Debug.Log($"知识库加载成功，共 {knowledgeEntries.Count} 条。");
        }
        catch (Exception e)
        {
            Debug.LogError("知识库解析失败: " + e.Message);
        }
    }

    private void OnInputChanged(string query)
    {

        if (string.IsNullOrWhiteSpace(query))
        {
            CancelPendingAsk();
            ShowHint();
            return;
        }

        if (!askOnValueChanged)
        {
            return;
        }

        CancelPendingAsk();
        pendingAskCoroutine = StartCoroutine(SubmitAfterDelay(query, debounceSeconds));
    }

    private IEnumerator SubmitAfterDelay(string query, float delay)
    {
        yield return new WaitForSeconds(delay);
        SubmitQuestion(query);
    }

    public void SubmitQuestion(string question)
    {
        string cleanQuestion = NormalizeText(question);
        if (string.IsNullOrWhiteSpace(cleanQuestion))
        {
            ShowHint();
            return;
        }

        if (Time.unscaledTime - lastSubmittedTime < 0.15f && cleanQuestion == lastSubmittedQuestion)
        {
            return;
        }

        lastSubmittedQuestion = cleanQuestion;
        lastSubmittedTime = Time.unscaledTime;

        CancelPendingAsk();
        ClearStepState();
        ClearResults();
        CreateResultItem("你：" + cleanQuestion, new Color(0.35f, 0.70f, 1f), true);
        CreateResultItem("AI 正在思考...", new Color(0.85f, 0.88f, 0.93f), false);

        if (useOnlineAI && HasUsableApiKey())
        {
            pendingAskCoroutine = StartCoroutine(SendQuestionToAI(cleanQuestion));
        }
        else
        {
            ShowLocalAnswer(cleanQuestion, null);
        }
    }

    public void SubmitCurrentQuestion()
    {
        if (inputField == null)
        {
            return;
        }

        SubmitQuestion(inputField.text);
    }

    private IEnumerator SendQuestionToAI(string userQuestion)
    {
        string prompt = BuildSystemPrompt();
        AIRequestData requestData = new AIRequestData
        {
            model = model,
            messages = new[]
            {
                new MessageItem { role = "system", content = prompt },
                new MessageItem { role = "user", content = userQuestion }
            },
            temperature = 0.2f
        };

        string jsonBody = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            activeRequest = request;
            request.timeout = Mathf.Max(1, Mathf.RoundToInt(requestTimeoutSeconds));
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return request.SendWebRequest();

            activeRequest = null;

            if (request.result == UnityWebRequest.Result.Success)
            {
                HandleSuccessResponse(request.downloadHandler.text, userQuestion);
            }
            else
            {
                Debug.LogWarning($"在线 AI 请求失败: {request.error}\n{request.downloadHandler.text}");
                ShowLocalAnswer(userQuestion, null);
            }
        }
    }

    private string BuildSystemPrompt()
    {
        string knowledge = string.IsNullOrWhiteSpace(loadedJsonData)
            ? "暂无本地知识库。"
            : RemoveControlCharacters(loadedJsonData);

        return "你是配电柜 AR 操作系统中的安全问答助手。" +
               "请只围绕配电柜巡检、停送电、故障处理、安全注意事项和本系统操作回答。" +
               "回答要使用简体中文，先给直接结论，再列出必要步骤；涉及带电、停送电、检修时必须提醒先断电、验电、挂牌并由有资质人员操作。" +
               "如果知识库没有依据，请明确说不确定，不要编造现场参数。" +
               "本地知识库如下：" + knowledge;
    }

    private void HandleSuccessResponse(string responseText, string userQuestion)
    {
        try
        {
            AIResponse response = JsonUtility.FromJson<AIResponse>(responseText);
            string answer = response?.choices != null && response.choices.Length > 0
                ? response.choices[0].message.content
                : "";

            if (string.IsNullOrWhiteSpace(answer))
            {
                ShowLocalAnswer(userQuestion, null);
                return;
            }

            ClearResults(); ClearStepState(); StartStepByStep(CleanAnswerForDisplay(answer));
        }
        catch (Exception e)
        {
            Debug.LogError("AI 响应解析失败: " + e.Message + "\n" + responseText);
            ShowLocalAnswer(userQuestion, null);
        }
    }

    private void ShowLocalAnswer(string userQuestion, string note)
    {
        var matches = FindAllMatches(userQuestion);

        if (!string.IsNullOrWhiteSpace(note))
        {
            Debug.Log(note);
        }

        // Remove only "AI thinking" text, keep the question
        ClearTemporaryItems();

        if (matches == null || matches.Count == 0)
        {
            CreateResultItem("本地知识库中没有找到足够匹配的流程。请换成更具体的问题。", new Color(0.90f, 0.92f, 0.96f), false);
            return;
        }

        if (matches.Count > 1)
        {
            ShowSelectionList(matches);
            return;
        }

        ShowAnswerSteps(matches[0]);
    }

    private void ShowAnswerSteps(OperationEntry best)
    {

        // Build step list directly from data
        List<string> steps = new List<string>();
        steps.Add(best.title ?? "未知标题");

        if (!string.IsNullOrWhiteSpace(best.command))
        {
            steps.Add("匹配指令：" + best.command);
        }

        if (best.steps != null && best.steps.Length > 0)
        {
            steps.Add("\u25a0 处理步骤：");
            for (int i = 0; i < best.steps.Length; i++)
            {
                steps.Add((i + 1) + ". " + (best.steps[i] ?? "(无内容)"));
            }
        }

        // Safety note as persistent
        string safetyNote = "\u26a0 安全提示：检修前请先断电、验电、挂牌，必要时联系有资质电工处理。";

        ClearStepState();
        pendingAnswerLines = steps;
        currentStepIndex = 0;

        // Show safety right away (persistent)
        GameObject safetyItem = CreateResultItemReturnObj(safetyNote, new Color(1f, 0.82f, 0.3f, 1f), false);
        if (safetyItem != null) persistentSafetyItems.Add(safetyItem);

        ShowNextStep();
    }

    public List<OperationEntry> SearchKnowledgeBase(string query)
    {
        return FindAllMatches(query);
    }

    private List<OperationEntry> FindAllMatches(string query)
    {
        if (knowledgeEntries.Count == 0) return null;

        string normalizedQuery = NormalizeText(query);
        var scored = new List<ScoredEntry>();

        foreach (OperationEntry entry in knowledgeEntries)
        {
            if (entry == null) continue;
            int score = ScoreEntry(normalizedQuery, entry);
            if (score >= 1) scored.Add(new ScoredEntry { entry = entry, score = score });
        }

        if (scored.Count == 0) return null;

        scored.Sort((a, b) => b.score.CompareTo(a.score));
        var matches = new List<OperationEntry>();
        foreach (var s in scored) matches.Add(s.entry);
        return matches;
    }

    private void ShowSelectionList(List<OperationEntry> matches)
    {
        if (matches == null || matches.Count == 0) return;

        isShowingSelection = true;
        selectionMatches = matches;
        ClearResults();
        ClearStepState();

        CreateResultItem("找到 " + matches.Count + " 个匹配结果，请选择：", new Color(0.35f, 0.70f, 1f), true);

        float uiScale = GetAiUiScale();
        Transform itemParent = resultContent != null ? resultContent : contentPanel;

        for (int i = 0; i < matches.Count; i++)
        {
            OperationEntry entry = matches[i];
            int idx = i;

            string label = (i + 1) + ". " + (entry.title ?? entry.command ?? "(无标题)");
            GameObject btnObj = new GameObject("_SelectBtn_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(itemParent, false);

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 1); btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(0.5f, 1);
            btnRect.sizeDelta = new Vector2(0, 34f * uiScale);

            LayoutElement le = btnObj.GetComponent<LayoutElement>();
            le.minHeight = 34f * uiScale; le.preferredHeight = 34f * uiScale;

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0.08f, 0.20f, 0.38f, 0.88f);
            btnImg.raycastTarget = true;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => HandleSelectionPicked(idx));

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            labelObj.transform.SetParent(btnObj.transform, false);
            TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 13f * uiScale;
            txt.color = new Color(0.85f, 0.90f, 0.98f, 1f);
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            txt.raycastTarget = false;
            txt.enableWordWrapping = true;
            if (chineseFont != null) txt.font = chineseFont;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f * uiScale, 0);
            labelRect.offsetMax = new Vector2(-8f * uiScale, 0);
        }

        try { if (resultContent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(resultContent); }
        catch { }
        if (resultScrollRect != null) resultScrollRect.verticalNormalizedPosition = 1f;
    }

    private void HandleSelectionPicked(int index)
    {
        if (selectionMatches == null || index < 0 || index >= selectionMatches.Count) return;
        OperationEntry picked = selectionMatches[index];
        isShowingSelection = false;
        selectionMatches = null;
        ClearResults();
        ClearStepState();
        ShowAnswerSteps(picked);
    }

    private OperationEntry FindBestLocalMatch(string query)
    {
        if (knowledgeEntries.Count == 0)
        {
            return null;
        }

        string normalizedQuery = NormalizeText(query);
        OperationEntry best = null;
        int bestScore = 0;

        foreach (OperationEntry entry in knowledgeEntries)
        {
            if (entry == null) { continue; }
            int score = ScoreEntry(normalizedQuery, entry);
            if (score > bestScore)
            {
                bestScore = score;
                best = entry;
            }
        }

        return bestScore >= 1 ? best : null;
    }

    private int ScoreEntry(string query, OperationEntry entry)
    {
        int score = 0;
        string searchable = NormalizeText(entry.command + " " + entry.title + " " + string.Join(" ", entry.keywords ?? Array.Empty<string>()) + " " + string.Join(" ", entry.steps ?? Array.Empty<string>()));

        if (!string.IsNullOrWhiteSpace(entry.command) && (query.Contains(NormalizeText(entry.command)) || NormalizeText(entry.command).Contains(query)))
        {
            score += 6;
        }

        if (!string.IsNullOrWhiteSpace(entry.title) && (query.Contains(NormalizeText(entry.title)) || NormalizeText(entry.title).Contains(query)))
        {
            score += 4;
        }

        foreach (string token in SplitTokens(query))
        {
            if (token.Length >= 2 && searchable.Contains(token))
            {
                score += 1;
            }
        }

        return score;
    }

    private IEnumerable<string> SplitTokens(string text)
    {
        char[] separators = { ' ', ',', '，', '.', '。', '?', '？', '!', '！', ';', '；', ':', '：', '\n', '\r', '\t' };
        foreach (string token in text.Split(separators, StringSplitOptions.RemoveEmptyEntries))
        {
            yield return token.Trim();
        }
    }

    private bool HasUsableApiKey()
    {
        return !string.IsNullOrWhiteSpace(apiKey) &&
               apiKey.Trim() != PlaceholderApiKey &&
               apiKey.Trim().StartsWith("sk-", StringComparison.OrdinalIgnoreCase);
    }

    private string WrapJsonArray(string json)
    {
        string trimmed = json.Trim();
        return trimmed.StartsWith("[", StringComparison.Ordinal) ? "{\"items\":" + trimmed + "}" : trimmed;
    }

    private string NormalizeText(string input)
    {
        return RemoveControlCharacters(input).Trim().ToLowerInvariant();
    }

    private string RemoveControlCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }

        StringBuilder sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (!char.IsControl(c) || c == '\n')
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private string StripMarkdownFence(string text)
    {
        return text.Replace("```json", "").Replace("```", "").Trim();
    }

    private string CleanAnswerForDisplay(string answer)
    {
        string text = StripMarkdownFence(answer ?? "");

        if (text.StartsWith("AI：", StringComparison.Ordinal))
        {
            text = text.Substring(3).TrimStart();
        }
        else if (text.StartsWith("AI:", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(3).TrimStart();
        }

        return text;
    }

    private void CreateAnswerItems(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            CreateResultItem("无返回内容，请重试。", new Color(0.90f, 0.92f, 0.96f), false);
            return;
        }

        string[] lines = answer.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            CreateResultItem(answer, new Color(0.90f, 0.92f, 0.96f), false);
            return;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line == null)
            {
                continue;
            }

            string trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            bool isHeader = i == 0;
            CreateResultItem(trimmed, isHeader ? new Color(0.35f, 0.70f, 1f) : new Color(0.90f, 0.92f, 0.96f), isHeader);
        }
    }

    private void CreateResultItem(string text, Color color, bool isHeader)
    {
        if (!EnsureContentPanelAvailable())
        {
            Debug.LogWarning("AI 问答结果 UI 未绑定完整。");
            return;
        }

        GameObject newItem = CreateFallbackTextItem();

        newItem.SetActive(true);
        Transform itemParent = resultContent != null ? resultContent : contentPanel;
        newItem.transform.SetParent(itemParent, false);

        TMP_Text txt = newItem.GetComponent<TMP_Text>();
        if (txt == null)
        {
            txt = newItem.GetComponentInChildren<TMP_Text>();
        }

        if (txt == null)
        {
            txt = newItem.AddComponent<TextMeshProUGUI>();
        }

        if (txt == null)
        {
            return;
        }

        txt.enabled = true;
        txt.gameObject.SetActive(true);
        txt.text = text;
        txt.color = color;
        txt.raycastTarget = false;
        float uiScale = GetAiUiScale();
        txt.fontSize = (isHeader ? 15f : 12.5f) * uiScale;
        txt.enableWordWrapping = true;
        txt.overflowMode = TextOverflowModes.Overflow;

        if (chineseFont != null)
        {
            txt.font = chineseFont;
        }

        RectTransform textRect = txt.rectTransform;
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 1);
        textRect.sizeDelta = new Vector2(0, (isHeader ? 26f : 22f) * uiScale);

        LayoutElement layoutElement = newItem.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = newItem.AddComponent<LayoutElement>();
        }

        txt.ForceMeshUpdate(true, true);
        float availableWidth = 120f * uiScale;
        if (itemParent is RectTransform panelRect)
        {
            availableWidth = Mathf.Max(90f * uiScale, panelRect.rect.width - 6f * uiScale);
        }
        else if (contentPanel is RectTransform contentRectForWidth)
        {
            availableWidth = Mathf.Max(90f * uiScale, contentRectForWidth.rect.width - 28f * uiScale);
        }

        float preferredHeight = Mathf.Ceil(txt.GetPreferredValues(text, availableWidth, 0f).y) + 6f * uiScale;
        preferredHeight = Mathf.Max(preferredHeight, (isHeader ? 28f : 24f) * uiScale);
        textRect.sizeDelta = new Vector2(0, preferredHeight);
        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;

        try
        {
            if (resultContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(resultContent);
            }

            if (contentPanel is RectTransform contentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("LayoutRebuilder 重建失败（可能因并发清理）: " + ex.Message);
        }

        if (resultScrollRect != null)
        {
            resultScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private GameObject CreateFallbackTextItem()
    {
        GameObject item = new GameObject("AIAnswerText", typeof(RectTransform));
        RectTransform rectTransform = item.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.sizeDelta = new Vector2(0, 24);
        return item;
    }

    private void ShowHint()
    {
        ClearStepState();
        ClearResults();
        CreateResultItem(emptyHint, new Color(0.85f, 0.88f, 0.93f), false);
    }

        private void ClearTemporaryItems()
    {
        if (contentPanel == null) return;
        Transform clearRoot = resultContent != null ? resultContent : contentPanel;
        // Only destroy the "AI thinking" text (last child) and next button
        for (int i = clearRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = clearRoot.GetChild(i);
            if (child == null) continue;
            if (child.name.StartsWith("_StepBtnBar") || (child.name == "AIAnswerText" && child.childCount == 0))
            {
                try { if (Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject); }
                catch { }
            }
            else if (i == clearRoot.childCount - 1)
            {
                try { if (Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject); }
                catch { }
                break; // only remove the last item (AI thinking text)
            }
        }
    }

    private void ClearResults()
    {
        if (contentPanel == null)
        {
            return;
        }

        Transform clearRoot = resultContent != null ? resultContent : contentPanel;

        int childCount = clearRoot.childCount;
        if (childCount == 0)
        {
            return;
        }

        Transform[] childrenToDestroy = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            childrenToDestroy[i] = clearRoot.GetChild(i);
        }

        for (int i = childrenToDestroy.Length - 1; i >= 0; i--)
        {
            Transform child = childrenToDestroy[i];
            if (child == null)
            {
                continue;
            }

            try
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("清除结果项时出错: " + ex.Message);
            }
        }
    }

    
    // ===== Step-by-step display =====

    private void ClearStepState()
    {
        pendingAnswerLines.Clear();
        currentStepIndex = 0;
        DestroyButtonBar();
        if (currentStepObj != null) { Destroy(currentStepObj); currentStepObj = null; }
        // Clear persistent safety items
        foreach (var item in persistentSafetyItems)
        {
            if (item != null) Destroy(item);
        }
        persistentSafetyItems.Clear();
    }

    private void StartStepByStep(string answer)
    {
        ClearStepState();
        string[] allLines = answer.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        pendingAnswerLines.Clear();
        List<string> safetyLines = new List<string>();

        foreach (var raw in allLines)
        {
            string trimmed = raw.Trim();
            if (trimmed.Length == 0) continue;
            if (trimmed.StartsWith("安全提示") || trimmed.StartsWith("安全：") || trimmed.StartsWith("注意"))
            {
                safetyLines.Add(trimmed);
            }
            else
            {
                pendingAnswerLines.Add(trimmed);
            }
        }

        // Show safety lines immediately as persistent items
        foreach (var safety in safetyLines)
        {
            GameObject item = CreateResultItemReturnObj(safety, new Color(1f, 0.82f, 0.3f, 1f), false);
            if (item != null) persistentSafetyItems.Add(item);
        }

        currentStepIndex = 0;
        ShowNextStep();
    }

    private void ShowNextStep()
    {
        if (currentStepIndex >= pendingAnswerLines.Count - 1) return;
        GoToStep(currentStepIndex + 1);
    }

    private void ShowPreviousStep()
    {
        if (currentStepIndex <= 0) return;
        GoToStep(currentStepIndex - 1);
    }

    private void GoToStep(int index)
    {
        if (index < 0 || index >= pendingAnswerLines.Count) return;

        // Destroy current step display
        if (currentStepObj != null) { Destroy(currentStepObj); currentStepObj = null; }
        DestroyButtonBar();

        currentStepIndex = index;
        string line = pendingAnswerLines[index];
        bool isHeader = index == 0;
        Color color = isHeader ? new Color(0.35f, 0.70f, 1f) : new Color(0.90f, 0.92f, 0.96f);
        currentStepObj = CreateResultItemReturnObj(line, color, isHeader);

        CreateButtonBar();
    }

    private void CreateButtonBar()
    {
        if (!EnsureContentPanelAvailable()) return;

        Transform itemParent = resultContent != null ? resultContent : contentPanel;
        float uiScale = GetAiUiScale();
        bool hasPrev = currentStepIndex > 0;
        bool hasNext = currentStepIndex < pendingAnswerLines.Count - 1;
        if (!hasPrev && !hasNext) return;

        // Bar container
        GameObject barObj = new GameObject("_StepBtnBar", typeof(RectTransform), typeof(CanvasRenderer));
        barObj.transform.SetParent(itemParent, false);
        RectTransform barRect = barObj.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 1); barRect.anchorMax = new Vector2(1, 1);
        barRect.pivot = new Vector2(0.5f, 1); barRect.sizeDelta = new Vector2(0, 34f * uiScale);

        LayoutElement barLE = barObj.AddComponent<LayoutElement>();
        barLE.minHeight = 34f * uiScale; barLE.preferredHeight = 34f * uiScale;

        if (hasPrev) CreateStepButton(barObj.transform, "\u25c0 \u4e0a\u4e00\u6b65", ShowPreviousStep, false, uiScale);
        if (hasNext) CreateStepButton(barObj.transform, "\u4e0b\u4e00\u6b65 \u25b6", ShowNextStep, true, uiScale);
    }

    private GameObject CreateStepButton(Transform parent, string label, UnityEngine.Events.UnityAction action, bool alignRight, float uiScale)
    {
        GameObject btnObj = new GameObject("_StepBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = alignRight ? new Vector2(0.5f, 0) : new Vector2(0, 0);
        btnRect.anchorMax = alignRight ? new Vector2(1, 0) : new Vector2(0.5f, 0);
        btnRect.pivot = new Vector2(0.5f, 0);
        btnRect.sizeDelta = new Vector2(0, 32f * uiScale);
        btnRect.anchoredPosition = Vector2.zero;

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.10f, 0.25f, 0.45f, 0.85f);
        btnImg.raycastTarget = true;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(action);

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelObj.transform.SetParent(btnObj.transform, false);
        TMP_Text txt = labelObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 13f * uiScale;
        txt.color = new Color(0.80f, 0.85f, 0.95f, 1f);
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;
        if (chineseFont != null) txt.font = chineseFont;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.sizeDelta = Vector2.zero;

        return btnObj;
    }

    private void DestroyButtonBar()
    {
        if (contentPanel == null) return;
        Transform clearRoot = resultContent != null ? resultContent : contentPanel;
        for (int i = clearRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = clearRoot.GetChild(i);
            if (child != null && child.name == "_StepBtnBar")
            {
                try { if (Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject); }
                catch { }
            }
        }
    }

    private void DestroyNextButton()
    {
        if (nextStepButton != null)
        {
            Destroy(nextStepButton);
            nextStepButton = null;
        }
    }

    private GameObject CreateResultItemReturnObj(string text, Color color, bool isHeader)
    {
        if (!EnsureContentPanelAvailable()) return null;

        GameObject newItem = CreateFallbackTextItem();
        newItem.SetActive(true);
        Transform itemParent = resultContent != null ? resultContent : contentPanel;
        newItem.transform.SetParent(itemParent, false);

        TMP_Text txt = newItem.GetComponent<TMP_Text>();
        if (txt == null) txt = newItem.GetComponentInChildren<TMP_Text>();
        if (txt == null) txt = newItem.AddComponent<TextMeshProUGUI>();
        if (txt == null) return null;

        txt.enabled = true;
        txt.gameObject.SetActive(true);
        txt.text = text;
        txt.color = color;
        txt.raycastTarget = false;
        float uiScale = GetAiUiScale();
        txt.fontSize = (isHeader ? 15f : 12.5f) * uiScale;
        txt.enableWordWrapping = true;
        txt.overflowMode = TextOverflowModes.Overflow;
        if (chineseFont != null) txt.font = chineseFont;

        RectTransform textRect = txt.rectTransform;
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 1);
        textRect.sizeDelta = new Vector2(0, (isHeader ? 26f : 22f) * uiScale);

        LayoutElement layoutElement = newItem.GetComponent<LayoutElement>();
        if (layoutElement == null) layoutElement = newItem.AddComponent<LayoutElement>();

        txt.ForceMeshUpdate(true, true);
        float availableWidth = 120f * uiScale;
        if (itemParent is RectTransform panelRect)
            availableWidth = Mathf.Max(90f * uiScale, panelRect.rect.width - 6f * uiScale);
        else if (contentPanel is RectTransform cr)
            availableWidth = Mathf.Max(90f * uiScale, cr.rect.width - 28f * uiScale);

        float preferredHeight = Mathf.Ceil(txt.GetPreferredValues(text, availableWidth, 0f).y) + 6f * uiScale;
        preferredHeight = Mathf.Max(preferredHeight, (isHeader ? 28f : 24f) * uiScale);
        textRect.sizeDelta = new Vector2(0, preferredHeight);
        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;

        try { if (resultContent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(resultContent); }
        catch { }

        if (resultScrollRect != null) resultScrollRect.verticalNormalizedPosition = 1f;
        return newItem;
    }

    private void CancelPendingAsk()
    {
        if (pendingAskCoroutine != null)
        {
            StopCoroutine(pendingAskCoroutine);
            pendingAskCoroutine = null;
        }

        if (activeRequest != null)
        {
            activeRequest.Abort();
            activeRequest.Dispose();
            activeRequest = null;
        }
    }

    [Serializable]
    public class OperationDataWrapper
    {
        public OperationEntry[] items;
    }

    [Serializable]
    public class OperationEntry
    {
        public string command;
        public string title;
        public string[] keywords;
        public string[] steps;
    }

    [Serializable]
    private class ScoredEntry
    {
        public OperationEntry entry;
        public int score;
    }

    public class AIRequestData
    {
        public string model;
        public MessageItem[] messages;
        public float temperature;
    }

    [Serializable]
    public class MessageItem
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class AIResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    public class Choice
    {
        public Message message;
    }

    [Serializable]
    public class Message
    {
        public string content;
    }
}







