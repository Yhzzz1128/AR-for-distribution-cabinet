using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class ToggleAnimOnTap : MonoBehaviour
{
    [Header("Animator (自动尝试查找)")]
    public Animator animator;

    [Header("Animator 参数名（必须和 Animator 一致）")]
    public string boolParameter = "isPlaying";

    [Header("初始是否播放")]
    public bool startPlaying = false;

    bool isPlaying = false;

    void Reset()
    {
        // 方便开发时自动填充
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        isPlaying = startPlaying;
        if (animator != null)
            animator.SetBool(boolParameter, isPlaying);
    }

    // 可以从射线管理器调用这个方法来切换播放/停止
    public void Toggle()
    {
        if (animator == null) return;
        isPlaying = !isPlaying;
        animator.SetBool(boolParameter, isPlaying);
    }

    // 方便直接在 Inspector 调试
    [ContextMenu("Play")]
    public void Play() { if (animator != null) { isPlaying = true; animator.SetBool(boolParameter, true); } }

    [ContextMenu("Stop")]
    public void Stop() { if (animator != null) { isPlaying = false; animator.SetBool(boolParameter, false); } }
}
