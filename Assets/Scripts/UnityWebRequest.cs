using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;

// 用于接收JSON数据的类 (必须与后端返回的JSON结构一致)
[Serializable]
public class OperationResponse
{
    public string status;
    public OperationData data;
}

[Serializable]
public class OperationData
{
    public string title;
    public StepData[] steps;
}

[Serializable]
public class StepData
{
    public int order;
    public string desc;
}

public class DatabaseConnector : MonoBehaviour
{
    // 你的后端API地址
    private string apiUrl = "https://your-api.com/get_operation?cmd="; 

    /// <summary>
    /// 根据指令从数据库获取流程
    /// </summary>
    public void FetchOperationFromDB(string command, Action<OperationData> onSuccess, Action<string> onFail)
    {
        // 开启协程进行网络请求
        StartCoroutine(GetRequest(apiUrl + command, onSuccess, onFail));
    }

    private IEnumerator GetRequest(string uri, Action<OperationData> onSuccess, Action<string> onFail)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // 发送请求
            yield return webRequest.SendWebRequest();

            // 处理结果
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // 解析JSON
                OperationResponse response = JsonUtility.FromJson<OperationResponse>(webRequest.downloadHandler.text);
                
                if (response.status == "success")
                {
                    onSuccess?.Invoke(response.data); // 成功，返回数据
                }
                else
                {
                    onFail?.Invoke("未找到该指令的操作流程");
                }
            }
            else
            {
                // 网络错误
                onFail?.Invoke("网络错误: " + webRequest.error);
            }
        }
    }
}