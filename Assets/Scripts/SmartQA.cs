using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SmartQA : MonoBehaviour
{
    [Header("Q&A UI")]
    public InputField questionInput;
    public Button askButton;
    public Text answerText;

    private List<KnowledgeItem> knowledgeBase = new List<KnowledgeItem>();

    [System.Serializable]
    private class KnowledgeItem
    {
        public string name;
        public string answer;
        public string[] keywords;
    }

    [System.Serializable]
    private class KnowledgeWrapper { public KnowledgeItem[] items; }

    void Awake()
    {
        LoadKnowledgeBase();
    }

    void Start()
    {
        if (answerText != null)
        {
            answerText.text = "Enter a question about buttons/indicators, then tap Ask.";
        }

        if (askButton != null)
        {
            askButton.onClick.AddListener(AskQuestion);
        }
    }

    void LoadKnowledgeBase()
    {
        knowledgeBase.Clear();

        // Try loading from Resources JSON first
        TextAsset json = Resources.Load<TextAsset>("SmartQA_Data");
        if (json != null)
        {
            string wrapped = json.text.Trim().StartsWith("[") ? "{\"items\":" + json.text + "}" : json.text;
            var wrapper = JsonUtility.FromJson<KnowledgeWrapper>(wrapped);
            if (wrapper != null && wrapper.items != null)
            {
                knowledgeBase.AddRange(wrapper.items);
                Debug.Log("[SmartQA] Loaded " + knowledgeBase.Count + " entries from Resources");
                return;
            }
        }

        // Fallback: inline knowledge
        Debug.LogWarning("[SmartQA] SmartQA_Data.json not found, using fallback inline data");
        BuildFallbackKnowledge();
    }

    void BuildFallbackKnowledge()
    {
        AddItem("电源指示", "电源指示：显示配电柜控制电源是否接通。\n正常：灯亮说明已上电。\n异常：灯不亮时检查上级电源、保险、接线。",
            "电源指示", "电源", "通电", "上电", "供电");
        AddItem("就地/停/远程按钮", "就地/停/远程按钮：选择控制方式。\n就地=现场按钮控制，停=停止状态，远程=PLC/上位机控制。\n切换前确认设备当前状态。",
            "就地", "远程", "停止模式", "控制方式", "模式");
        AddItem("就地开阀按钮", "就地开阀按钮：在就地模式下控制阀门开启。\n操作前确认：电源正常、处于就地模式、无故障报警。\n操作后观察：开阀运行指示→开到位指示。",
            "就地开阀", "开阀按钮", "开阀", "打开阀门", "开启");
        AddItem("就地关阀按钮", "就地关阀按钮：在就地模式下控制阀门关闭。\n操作前确认：电源正常、处于就地模式、允许关闭。\n操作后观察：关阀运行指示→关闭位指示。",
            "就地关阀", "关阀按钮", "关阀", "关闭阀门", "关闭");
        AddItem("就地停止按钮", "就地停止按钮：停止正在进行的开阀或关阀动作。\n适用：暂停、发现异常、中断过程。\n按下后阀门停在当前位置。",
            "就地停止", "停止按钮", "停止", "停机", "暂停");
        AddItem("关闭位指示", "关闭位指示：阀门已到达全关位置。\n正常：关阀→关阀运行→全关→关闭位指示亮。\n异常：长时间不亮可能未关到位、限位开关异常或卡阻。",
            "关闭位", "关到位", "全关", "关闭位指示");
        AddItem("开到位指示", "开到位指示：阀门已到达全开位置。\n正常：开阀→开阀运行→全开→开到位指示亮。\n异常：长时间不亮可能未开到位、限位开关异常或卡阻。",
            "开到位", "全开", "开启到位", "开到位指示");
        AddItem("关阀运行指示", "关阀运行指示：阀门正在执行关阀动作。\n正常：按关阀按钮→运行灯亮→到位后灯灭→关闭位灯亮。\n异常：灯一直亮但不关到位可能是卡滞或信号异常。",
            "关阀运行", "关阀运行指示", "正在关阀");
        AddItem("开阀运行指示", "开阀运行指示：阀门正在执行开阀动作。\n正常：按开阀按钮→运行灯亮→到位后灯灭→开到位灯亮。\n异常：灯一直亮但不开到位可能是卡滞或信号异常。",
            "开阀运行", "开通运行", "正在开阀");
    }

    void AddItem(string name, string answer, params string[] keywords)
    {
        knowledgeBase.Add(new KnowledgeItem { name = name, answer = answer, keywords = keywords });
    }

    public void AskQuestion()
    {
        if (questionInput == null || answerText == null)
        {
            Debug.LogError("[SmartQA] UI not fully bound - check Inspector");
            return;
        }

        string question = questionInput.text.Trim();

        if (string.IsNullOrEmpty(question))
        {
            answerText.text = "Please enter a question first.\n\nExample: What is the power indicator?\nYou can also ask about: open valve button, close valve button, stop button, position indicators, etc.";
            return;
        }

        string result = GetAnswer(question);
        answerText.text = result;

        if (result.Contains("no matching") || result.Contains("not match"))
        {
            // Highlight feedback by prepending indicator
            answerText.text = "[No Match]\n\n" + result;
        }
    }

    private string GetAnswer(string question)
    {
        string q = Normalize(question);

        if (ContainsAny(q, "all button", "list", "what button", "introduce", "function list", "button function"))
        {
            return GetButtonListAnswer();
        }

        if (ContainsAny(q, "standard", "operation", "procedure", "normal", "step"))
        {
            return GetStandardOperationAnswer();
        }

        if (ContainsAny(q, "fault", "abnormal", "no response", "not work", "not light", "alarm", "handle", "problem", "broken"))
        {
            return GetFaultAnswer();
        }

        KnowledgeItem best = FindBestItem(q);

        if (best != null)
        {
            return best.answer;
        }

        return "No matching answer found in the knowledge base.\n\n" +
            "Try another way of asking, for example:\n" +
            "1. What is the power indicator?\n" +
            "2. What does local/stop/remote mean?\n" +
            "3. How to use the open valve button?\n" +
            "4. How to use the close valve button?\n" +
            "5. What does the stop button do?\n" +
            "6. What does the open position indicator mean?\n" +
            "7. What does the closed position indicator mean?\n" +
            "8. What does the open running indicator do?\n" +
            "9. What if the close running indicator stays on?";
    }

    private KnowledgeItem FindBestItem(string q)
    {
        KnowledgeItem bestItem = null;
        int bestScore = 0;

        foreach (KnowledgeItem item in knowledgeBase)
        {
            int score = 0;
            if (item.keywords == null) continue;

            foreach (string keyword in item.keywords)
            {
                string k = Normalize(keyword);
                if (q.Contains(k)) score += 100 + k.Length;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestItem = item;
            }
        }

        return bestScore >= 50 ? bestItem : null;
    }

    private string GetButtonListAnswer()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[All Buttons & Indicators]");
        sb.AppendLine();
        sb.AppendLine("1. Power indicator - shows device power status");
        sb.AppendLine("2. Local/Stop/Remote - selects control mode");
        sb.AppendLine("3. Local open valve - opens valve in local mode");
        sb.AppendLine("4. Local close valve - closes valve in local mode");
        sb.AppendLine("5. Local stop - stops current open/close action");
        sb.AppendLine("6. Open position indicator - valve fully open");
        sb.AppendLine("7. Closed position indicator - valve fully closed");
        sb.AppendLine("8. Open running indicator - valve opening in progress");
        sb.AppendLine("9. Close running indicator - valve closing in progress");
        sb.AppendLine();
        sb.AppendLine("Enter a specific button name for detailed info.");
        return sb.ToString();
    }

    private string GetStandardOperationAnswer()
    {
        return "[Standard Operation Procedure]\n\n" +
            "1. Confirm power indicator is on (device powered)\n" +
            "2. Select control mode: Local, Stop, or Remote\n" +
            "3. For local operation, switch mode to Local\n" +
            "4. Press open valve to open, close valve to close\n" +
            "5. Observe running indicator during operation\n" +
            "6. Confirm position indicator after action completes\n" +
            "7. Press stop button if any abnormality occurs";
    }

    private string GetFaultAnswer()
    {
        return "[Common Faults & Handling]\n\n" +
            "1. Power indicator off: check upstream power, fuses, wiring\n" +
            "2. Buttons no response: check if in Local mode (Remote disables local buttons)\n" +
            "3. Valve won't open: check open conditions, interlocks, actuator, control circuit\n" +
            "4. Valve won't close: check close conditions, interlocks, actuator, control circuit\n" +
            "5. Running indicator stays on: possible valve jam, limit switch fault, or actuator fault\n" +
            "6. Position indicator not lighting: check limit switch, feedback wiring, actual valve position\n\n" +
            "Principle: check power first, then mode, then button, then interlock, then actuator, then feedback";
    }

    private bool ContainsAny(string q, params string[] words)
    {
        foreach (string word in words)
        {
            if (q.Contains(Normalize(word))) return true;
        }
        return false;
    }

    private string Normalize(string text)
    {
        if (text == null) return "";
        return text.Replace(" ", "").Replace(".", "").Replace(",", "").Replace("?", "")
            .Replace("!", "").Replace("/", "").Replace(":", "").ToLower();
    }
}
