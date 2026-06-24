using UnityEngine;
using easyar;

public class TargetAutoClose : MonoBehaviour
{
    private ImageTargetController target;

    void Start()
    {
        target = GetComponent<ImageTargetController>();
        if (target != null)
        {
            target.TargetLost += OnTargetLost;
        }
    }

    private void OnTargetLost()
    {
        if (QAPanelController.Instance != null)
        {
            QAPanelController.Instance.HideQA();
        }
    }

    void OnDestroy()
    {
        if (target != null)
        {
            target.TargetLost -= OnTargetLost;
        }
    }
}
