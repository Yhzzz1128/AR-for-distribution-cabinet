using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Text;

public class QAPageController : MonoBehaviour
{
    public TMP_FontAsset menuFont;
    public Canvas canvas;
    public Action OnBackToMenu;

    private GameObject pagePanel;
    private GameObject searchBtnObj;
    private GameObject statusObj;
    private Transform searchOriginalParent;
    private Transform resultOriginalParent;
    private Vector2 searchOrigPos, searchOrigSize;
    private Vector2 resultOrigPos, resultOrigSize;

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
        // Restore search UI to original positions
        RestoreSearchUI();
        if (searchBtnObj != null) searchBtnObj.SetActive(false);
        if (statusObj != null) statusObj.SetActive(false);
    }

    void CreatePage()
    {
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogError("[QAPage] No Canvas found"); return; }

        float s = Mathf.Clamp(Screen.width / 540f, 1f, 2f);

        // Full-screen background
        pagePanel = new GameObject("QAPage", typeof(RectTransform), typeof(Image));
        pagePanel.transform.SetParent(canvas.transform, false);
        pagePanel.transform.SetAsLastSibling();
        RectTransform pr = pagePanel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        pagePanel.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.12f, 1f);

        // Top bar
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(pagePanel.transform, false);
        RectTransform tb = topBar.GetComponent<RectTransform>();
        tb.anchorMin = new Vector2(0, 1); tb.anchorMax = new Vector2(1, 1);
        tb.pivot = new Vector2(0.5f, 1);
        tb.sizeDelta = new Vector2(0, 56f * s); tb.anchoredPosition = Vector2.zero;
        topBar.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.18f, 0.95f);

        // Back button
        MakeBtn(topBar.transform, "Back", new Vector2(12f * s, -28f * s), new Vector2(70f * s, 36f * s),
            new Color(0.08f, 0.15f, 0.35f, 0.9f), new Color(0.75f, 0.80f, 0.95f, 1f), 12f * s,
            () => { OnBackToMenu?.Invoke(); });

        // Title
        GameObject titleObj = new GameObject("PageTitle", typeof(RectTransform));
        titleObj.transform.SetParent(topBar.transform, false);
        TMP_Text titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "Knowledge Base";
        titleTxt.fontSize = 18f * s; titleTxt.color = new Color(0.35f, 0.70f, 1f, 1f);
        titleTxt.alignment = TextAlignmentOptions.Center; titleTxt.fontStyle = FontStyles.Bold;
        if (menuFont != null) titleTxt.font = menuFont;
        RectTransform tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = new Vector2(0, -28f * s); tr.sizeDelta = new Vector2(200f * s, 36f * s);

        // Search button (reliable trigger)
        searchBtnObj = new GameObject("QASearchBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        searchBtnObj.transform.SetParent(pagePanel.transform, false);
        RectTransform sbr = searchBtnObj.GetComponent<RectTransform>();
        sbr.anchorMin = new Vector2(0.5f, 1f); sbr.anchorMax = new Vector2(0.5f, 1f);
        sbr.pivot = new Vector2(0.5f, 1f);
        sbr.anchoredPosition = new Vector2(0, -64f * s); sbr.sizeDelta = new Vector2(160f * s, 44f * s);
        searchBtnObj.GetComponent<Image>().color = new Color(0.10f, 0.30f, 0.55f, 0.9f);
        searchBtnObj.GetComponent<Button>().onClick.AddListener(DoSearch);

        GameObject btnLabel = new GameObject("Label", typeof(RectTransform));
        btnLabel.transform.SetParent(searchBtnObj.transform, false);
        TMP_Text btnTxt = btnLabel.AddComponent<TextMeshProUGUI>();
        btnTxt.text = "Search Knowledge Base";
        btnTxt.fontSize = 15f * s; btnTxt.color = new Color(0.90f, 0.93f, 0.98f, 1f);
        btnTxt.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) btnTxt.font = menuFont;
        RectTransform blr = btnLabel.GetComponent<RectTransform>();
        blr.anchorMin = Vector2.zero; blr.anchorMax = Vector2.one; blr.sizeDelta = Vector2.zero;

        // Status label
        statusObj = new GameObject("QAStatus", typeof(RectTransform));
        statusObj.transform.SetParent(pagePanel.transform, false);
        TMP_Text statTxt = statusObj.AddComponent<TextMeshProUGUI>();
        statTxt.text = "Click Search to find information";
        statTxt.fontSize = 11f * s; statTxt.color = new Color(0.40f, 0.45f, 0.55f, 0.7f);
        statTxt.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) statTxt.font = menuFont;
        RectTransform str = statusObj.GetComponent<RectTransform>();
        str.anchorMin = new Vector2(0.5f, 1f); str.anchorMax = new Vector2(0.5f, 1f);
        str.pivot = new Vector2(0.5f, 1f);
        str.anchoredPosition = new Vector2(0, -120f * s); str.sizeDelta = new Vector2(300f * s, 24f * s);

        // Move existing search input into this page
        RepositionSearchUI(s);

        // Move existing result panel into this page
        RepositionResultUI(s);
    }

    void RepositionSearchUI(float s)
    {
        var sousuo = GameObject.Find("sousuo");
        if (sousuo == null) { Debug.LogWarning("[QAPage] sousuo not found"); return; }

        searchOriginalParent = sousuo.transform.parent;
        RectTransform sr = sousuo.GetComponent<RectTransform>();
        if (sr == null) sr = sousuo.AddComponent<RectTransform>();
        searchOrigPos = sr.anchoredPosition;
        searchOrigSize = sr.sizeDelta;

        sousuo.transform.SetParent(pagePanel.transform, false);
        sr.anchorMin = new Vector2(0.5f, 1f); sr.anchorMax = new Vector2(0.5f, 1f);
        sr.pivot = new Vector2(0.5f, 1f);
        sr.anchoredPosition = new Vector2(0, -118f * s);
        sr.sizeDelta = new Vector2(320f * s, 44f * s);
    }

    void RepositionResultUI(float s)
    {
        var resultPanel = GameObject.Find("ResultPanel");
        if (resultPanel == null) { Debug.LogWarning("[QAPage] ResultPanel not found"); return; }

        resultOriginalParent = resultPanel.transform.parent;
        RectTransform rr = resultPanel.GetComponent<RectTransform>();
        if (rr == null) rr = resultPanel.AddComponent<RectTransform>();
        resultOrigPos = rr.anchoredPosition;
        resultOrigSize = rr.sizeDelta;

        resultPanel.transform.SetParent(pagePanel.transform, false);
        rr.anchorMin = new Vector2(0.5f, 1f); rr.anchorMax = new Vector2(0.5f, 1f);
        rr.pivot = new Vector2(0.5f, 1f);
        rr.anchoredPosition = new Vector2(0, -172f * s);
        rr.sizeDelta = new Vector2(340f * s, 340f * s);
    }

    void RestoreSearchUI()
    {
        var sousuo = GameObject.Find("sousuo");
        if (sousuo != null && searchOriginalParent != null)
        {
            sousuo.transform.SetParent(searchOriginalParent, false);
            RectTransform sr = sousuo.GetComponent<RectTransform>();
            if (sr != null) { sr.anchoredPosition = searchOrigPos; sr.sizeDelta = searchOrigSize; }
        }
        var resultPanel = GameObject.Find("ResultPanel");
        if (resultPanel != null && resultOriginalParent != null)
        {
            resultPanel.transform.SetParent(resultOriginalParent, false);
            RectTransform rr = resultPanel.GetComponent<RectTransform>();
            if (rr != null) { rr.anchoredPosition = resultOrigPos; rr.sizeDelta = resultOrigSize; }
        }
    }

    void DoSearch()
    {
        var mgr = FindObjectOfType<AI_Search_Manager>();
        if (mgr == null)
        {
            if (statusObj != null)
            {
                var txt = statusObj.GetComponent<TMP_Text>();
                if (txt != null) txt.text = "Search engine not available";
            }
            return;
        }

        // Trigger search through the existing search manager
        mgr.SubmitCurrentQuestion();
    }

    void MakeBtn(Transform parent, string label, Vector2 pos, Vector2 size,
        Color bg, Color tc, float fs, Action action)
    {
        GameObject btn = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
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
