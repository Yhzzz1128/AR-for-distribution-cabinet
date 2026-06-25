using TMPro;
using UnityEngine;

public class FaultChallengeManager : MonoBehaviour
{
    [Header("UI 显示")]
    public TMP_Text challengeText;

    [Header("挑战状态")]
    public bool challengeMode = false;

    private string currentFaultName;
    private string currentHint;
    private string currentDescription;
    private string correctKey;
    private string correctPartName;
    private int score = 0;
    private int total = 0;

    public string CurrentFaultName => currentFaultName;
    public string CurrentDescription => currentDescription;
    public string CurrentHint => currentHint;
    public string ScoreSummary => $"{score} / {total}";
    public string ScoreLine => $"当前得分：{score} / {total}";
    public bool HasProgress => total > 0;

    void Awake()
    {
        ResetState();
    }

    public void StartIndicatorFault()
    {
        PrepareChallenge(
            "指示灯不亮",
            "运行面板的状态指示灯完全熄灭，请选出最需要优先排查的部件。",
            "提示：优先检查指示灯本体、指示灯供电回路、熔断器以及相关接线端子。",
            "indicator",
            "指示灯");
    }

    public void StartBreakerFault()
    {
        PrepareChallenge(
            "断路器跳闸",
            "主回路突然断电，断路器处于跳闸状态，请选择最优先排查的部件。",
            "提示：重点关注断路器、负载回路、上游短路及过载情况。",
            "breaker",
            "断路器");
    }

    public void StartSmellFault()
    {
        PrepareChallenge(
            "柜内有烧焦味",
            "配电柜内部出现明显的烧焦气味，请选择最优先排查的部件。",
            "提示：检查接线端子、母排、电缆接头以及可能存在发热的部位。",
            "terminal",
            "接线端子或母排");
    }

    public bool TryEvaluateSelection(string selectedKey, string selectedName, out string feedback, out bool correct)
    {
        feedback = string.Empty;
        correct = false;

        if (!challengeMode)
        {
            feedback = "请先选择挑战主题，再作答。";
            ShowMessage(feedback);
            return false;
        }

        total++;

        if (!string.IsNullOrEmpty(selectedKey) && selectedKey == correctKey)
        {
            score++;
            correct = true;
            feedback =
                $"【回答正确】\n\n挑战：{currentFaultName}\n你的选择：{selectedName}\n\n说明：优先检查“{correctPartName}”能够最快确认故障原因。\n\n{ScoreLine}";
        }
        else
        {
            feedback =
                $"【回答不正确】\n\n挑战：{currentFaultName}\n你的选择：{selectedName}\n\n首选排查部件：{correctPartName}\n\n{currentHint}\n\n{ScoreLine}";
        }

        ShowMessage(feedback);
        return true;
    }

    public bool HandleARButtonClick(ClickableButton3D button)
    {
        if (button == null) return false;

        string clickedKey = button.faultKey;
        string clickedName = string.IsNullOrEmpty(button.displayName) ? button.gameObject.name : button.displayName;

        return TryEvaluateSelection(clickedKey, clickedName, out _, out _);
    }

    public void ExitChallenge()
    {
        if (challengeMode)
        {
            ShowMessage($"【挑战结束】\n\n最终成绩：{ScoreSummary}\n\n可重新选择挑战继续练习。");
        }

        challengeMode = false;
    }

    private void PrepareChallenge(string title, string description, string hint, string key, string partName)
    {
        score = 0;
        total = 0;
        currentFaultName = title;
        currentDescription = description;
        currentHint = hint;
        correctKey = key;
        correctPartName = partName;
        challengeMode = true;

        ShowMessage(BuildStartMessage());
    }

    private void ResetState()
    {
        currentFaultName = string.Empty;
        currentDescription = string.Empty;
        currentHint = string.Empty;
        correctKey = string.Empty;
        correctPartName = string.Empty;
        score = 0;
        total = 0;
        challengeMode = false;
    }

    private string BuildStartMessage()
    {
        return $"【挑战开始】\n\n主题：{currentFaultName}\n{currentDescription}\n\n{currentHint}\n\n{ScoreLine}";
    }

    private void ShowMessage(string msg)
    {
        if (challengeText != null)
        {
            challengeText.text = msg;
        }
        else
        {
            Debug.Log(msg);
        }
    }
}
