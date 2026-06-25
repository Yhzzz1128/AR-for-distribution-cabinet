using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SmartQA : MonoBehaviour
{
    [Header("问答系统 UI")]
    public InputField questionInput;
    public Button askButton;
    public Text answerText;

    private List<KnowledgeItem> knowledgeBase = new List<KnowledgeItem>();

    private class KnowledgeItem
    {
        public string name;
        public string answer;
        public string[] keywords;

        public KnowledgeItem(string name, string answer, string[] keywords)
        {
            this.name = name;
            this.answer = answer;
            this.keywords = keywords;
        }
    }

    void Awake()
    {
        BuildKnowledgeBase();
    }

    void Start()
    {
        if (answerText != null)
        {
            answerText.text = "请输入问题后点击提问，系统会根据配电柜按钮知识库进行回答。";
        }

        if (askButton != null)
        {
            askButton.onClick.AddListener(AskQuestion);
        }
    }

    public void AskQuestion()
    {
        if (questionInput == null || answerText == null)
        {
            Debug.LogError("问答系统 UI 没有绑定完整，请检查 Inspector。");
            return;
        }

        string question = questionInput.text.Trim();

        if (string.IsNullOrEmpty(question))
        {
            answerText.text = "请先输入问题，例如：电源指示有什么用？";
            return;
        }

        answerText.text = GetAnswer(question);
    }

    private string GetAnswer(string question)
    {
        string q = Normalize(question);

        if (ContainsAny(q, "所有按钮", "全部按钮", "有哪些按钮", "按钮介绍", "功能列表", "按钮功能"))
        {
            return GetButtonListAnswer();
        }

        if (ContainsAny(q, "标准操作", "操作流程", "怎么操作", "正常操作", "使用步骤"))
        {
            return GetStandardOperationAnswer();
        }

        if (ContainsAny(q, "故障", "异常", "没反应", "不动作", "不亮", "报警", "处理"))
        {
            return GetFaultAnswer();
        }

        KnowledgeItem best = FindBestItem(q);

        if (best != null)
        {
            return best.answer;
        }

        return
            "知识库暂时没有匹配到你的问题。\n\n" +
            "你可以换一种问法，例如：\n" +
            "1. 电源指示有什么用？\n" +
            "2. 就地/停/远程按钮是什么意思？\n" +
            "3. 就地开阀按钮怎么用？\n" +
            "4. 就地关阀按钮怎么用？\n" +
            "5. 就地停止按钮有什么作用？\n" +
            "6. 开到位指示亮了代表什么？\n" +
            "7. 关闭位指示是什么意思？\n" +
            "8. 开阀运行指示有什么作用？\n" +
            "9. 关阀运行指示亮了怎么办？";
    }

    private KnowledgeItem FindBestItem(string q)
    {
        KnowledgeItem bestItem = null;
        int bestScore = 0;

        foreach (KnowledgeItem item in knowledgeBase)
        {
            int score = 0;

            foreach (string keyword in item.keywords)
            {
                string k = Normalize(keyword);

                if (q.Contains(k))
                {
                    score += 100 + k.Length;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestItem = item;
            }
        }

        if (bestScore >= 50)
        {
            return bestItem;
        }

        return null;
    }

    private void BuildKnowledgeBase()
    {
        knowledgeBase.Clear();

        AddItem(
            "电源指示",
            "【电源指示】\n\n" +
            "作用：用于显示配电柜控制电源或设备电源是否已经接通。\n\n" +
            "正常情况：电源指示灯亮，说明设备已经上电，可以进行后续操作。\n\n" +
            "异常情况：如果电源指示灯不亮，应先检查上级电源、控制电源、保险、空气开关和接线情况，不建议直接操作开阀或关阀按钮。",
            "电源指示", "电源", "通电", "上电", "供电", "电源灯", "电源指示灯"
        );

        AddItem(
            "就地/停/远程按钮",
            "【就地/停/远程按钮】\n\n" +
            "作用：用于选择配电柜或阀门设备的控制方式。\n\n" +
            "就地：表示由现场按钮直接控制设备，适合现场调试和人工操作。\n\n" +
            "停：表示设备处于停止或禁止动作状态，适合暂停、检修或安全保护。\n\n" +
            "远程：表示设备由 PLC、上位机或远程控制系统控制，现场按钮通常不作为主要控制方式。\n\n" +
            "注意：切换控制模式前，应确认设备当前状态，避免运行过程中误切换导致异常。",
            "就地", "远程", "停止模式", "就地停远程", "就地/停/远程", "控制方式", "模式", "本地", "远控"
        );

        AddItem(
            "就地开阀按钮",
            "【就地开阀按钮】\n\n" +
            "作用：在就地控制模式下，用于发出开阀命令，使阀门向开启方向动作。\n\n" +
            "操作前应确认：\n" +
            "1. 电源指示正常；\n" +
            "2. 控制方式处于就地；\n" +
            "3. 无故障报警；\n" +
            "4. 现场具备开阀条件。\n\n" +
            "操作后应观察：开阀运行指示应亮起；阀门完全打开后，开到位指示应亮起。",
            "就地开阀", "开阀按钮", "开阀", "打开阀门", "开启", "打开", "开通"
        );

        AddItem(
            "就地关阀按钮",
            "【就地关阀按钮】\n\n" +
            "作用：在就地控制模式下，用于发出关阀命令，使阀门向关闭方向动作。\n\n" +
            "操作前应确认：\n" +
            "1. 电源指示正常；\n" +
            "2. 控制方式处于就地；\n" +
            "3. 阀门允许关闭；\n" +
            "4. 没有联锁限制或机械卡阻。\n\n" +
            "操作后应观察：关阀运行指示应亮起；阀门完全关闭后，关闭位指示应亮起。",
            "就地关阀", "关阀按钮", "关阀", "关闭阀门", "关闭", "关门"
        );

        AddItem(
            "就地停止按钮",
            "【就地停止按钮】\n\n" +
            "作用：用于停止当前正在进行的开阀或关阀动作。\n\n" +
            "适用情况：\n" +
            "1. 阀门运行过程中需要暂停；\n" +
            "2. 操作人员发现异常；\n" +
            "3. 需要中断开阀或关阀过程。\n\n" +
            "按下后，阀门一般会停止在当前位置。再次操作前，应确认异常已经排除。",
            "就地停止", "停止按钮", "停止", "停机", "暂停", "中止", "停止运行"
        );

        AddItem(
            "关闭位指示",
            "【关闭位指示】\n\n" +
            "作用：用于显示阀门已经到达全关位置。\n\n" +
            "正常情况：执行关阀操作后，关阀运行指示先亮；阀门完全关闭后，关闭位指示亮起。\n\n" +
            "异常情况：如果长时间没有关闭位指示，可能是阀门未关到位、限位开关异常、机械卡阻或控制线路异常。",
            "关闭位", "关到位", "全关", "关闭位指示", "关位", "关位指示"
        );

        AddItem(
            "开到位指示",
            "【开到位指示】\n\n" +
            "作用：用于显示阀门已经到达全开位置。\n\n" +
            "正常情况：执行开阀操作后，开阀运行指示先亮；阀门完全打开后，开到位指示亮起。\n\n" +
            "异常情况：如果长时间没有开到位指示，可能是阀门未开到位、限位开关异常、阀门卡滞或控制回路异常。",
            "开到位", "全开", "开启到位", "开到位指示", "开位", "开位指示"
        );

        AddItem(
            "关阀运行指示",
            "【关阀运行指示】\n\n" +
            "作用：用于显示阀门正在执行关阀动作。\n\n" +
            "正常情况：按下就地关阀按钮后，关阀运行指示亮起；阀门到达关闭位置后，关阀运行指示熄灭，关闭位指示亮起。\n\n" +
            "异常情况：如果关阀运行指示一直亮，但关闭位指示不亮，可能是阀门卡滞、关到位信号异常或执行机构故障。",
            "关阀运行", "关阀运行指示", "正在关阀", "关阀灯", "关阀过程", "关阀动作"
        );

        AddItem(
            "开阀运行指示",
            "【开阀运行指示】\n\n" +
            "作用：用于显示阀门正在执行开阀动作。\n\n" +
            "正常情况：按下就地开阀按钮后，开阀运行指示亮起；阀门到达开启位置后，开阀运行指示熄灭，开到位指示亮起。\n\n" +
            "异常情况：如果开阀运行指示一直亮，但开到位指示不亮，可能是阀门卡滞、开到位信号异常、执行机构故障或控制线路异常。",
            "开阀运行", "开通运行", "开通运行指示", "正在开阀", "开阀灯", "开阀过程", "开阀动作"
        );
    }

    private void AddItem(string name, string answer, params string[] keywords)
    {
        knowledgeBase.Add(new KnowledgeItem(name, answer, keywords));
    }

    private string GetButtonListAnswer()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("【配电柜按钮与指示灯功能总览】");
        sb.AppendLine();
        sb.AppendLine("1. 电源指示：显示设备是否已经上电。");
        sb.AppendLine("2. 就地/停/远程按钮：选择现场控制、停止状态或远程控制。");
        sb.AppendLine("3. 就地开阀按钮：在就地模式下控制阀门开启。");
        sb.AppendLine("4. 就地关阀按钮：在就地模式下控制阀门关闭。");
        sb.AppendLine("5. 就地停止按钮：停止当前开阀或关阀动作。");
        sb.AppendLine("6. 开到位指示：显示阀门已经完全打开。");
        sb.AppendLine("7. 关闭位指示：显示阀门已经完全关闭。");
        sb.AppendLine("8. 开阀运行指示：显示阀门正在执行开启动作。");
        sb.AppendLine("9. 关阀运行指示：显示阀门正在执行关闭动作。");
        sb.AppendLine();
        sb.AppendLine("你可以继续输入某个按钮名称，系统会给出更详细解释。");

        return sb.ToString();
    }

    private string GetStandardOperationAnswer()
    {
        return
            "【标准操作流程】\n\n" +
            "1. 先确认电源指示灯正常，说明设备已经上电。\n" +
            "2. 根据需要选择控制模式：就地、停或远程。\n" +
            "3. 如果需要现场操作，将模式切换到就地。\n" +
            "4. 需要打开阀门时，按下就地开阀按钮。\n" +
            "5. 需要关闭阀门时，按下就地关阀按钮。\n" +
            "6. 阀门运行过程中观察开阀运行指示或关阀运行指示。\n" +
            "7. 阀门动作结束后，确认开到位指示或关闭位指示是否正常亮起。\n" +
            "8. 如果发现异常，可以按就地停止按钮中断动作，并进行故障检查。";
    }

    private string GetFaultAnswer()
    {
        return
            "【常见故障处理】\n\n" +
            "1. 电源指示不亮：检查上级电源、控制电源、保险、接线和电源开关。\n" +
            "2. 按钮无反应：检查是否处于就地模式，远程模式下现场按钮可能不起作用。\n" +
            "3. 开阀无动作：检查开阀条件、联锁信号、执行机构和开阀控制回路。\n" +
            "4. 关阀无动作：检查关阀条件、联锁信号、执行机构和关阀控制回路。\n" +
            "5. 运行指示一直亮：可能存在阀门卡阻、到位开关异常或执行机构故障。\n" +
            "6. 到位指示不亮：检查限位开关、反馈线路和阀门实际位置。\n\n" +
            "处理原则：先确认电源，再确认模式，然后检查按钮、联锁、执行机构和到位反馈。";
    }

    private bool ContainsAny(string q, params string[] words)
    {
        foreach (string word in words)
        {
            if (q.Contains(Normalize(word)))
            {
                return true;
            }
        }

        return false;
    }

    private string Normalize(string text)
    {
        if (text == null)
        {
            return "";
        }

        return text
            .Replace(" ", "")
            .Replace("　", "")
            .Replace("？", "")
            .Replace("?", "")
            .Replace("。", "")
            .Replace(".", "")
            .Replace("，", "")
            .Replace(",", "")
            .Replace("！", "")
            .Replace("!", "")
            .Replace("/", "")
            .Replace("\\", "")
            .Replace("：", "")
            .Replace(":", "")
            .ToLower();
    }
}