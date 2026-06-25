using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ButtonQAPageController : MonoBehaviour
{
    public TMP_FontAsset menuFont;
    public Canvas canvas;
    public System.Action OnBackToMenu;

    private GameObject pagePanel;
    private GameObject detailPanel;
    private TextMeshProUGUI detailText;
    private TextMeshProUGUI statusText;

    private class BtnInfo { public string name; public string answer; }
    private List<BtnInfo> buttons = new List<BtnInfo>();

    [System.Serializable] private class BtnData { public string name; public string answer; }
    [System.Serializable] private class BtnWrapper { public BtnData[] items; }

    public void Show()
    {
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogError("[BtnQA] No Canvas"); return; }
        if (pagePanel != null) { pagePanel.SetActive(true); return; }

        LoadData();
        if (buttons.Count == 0) { Debug.LogError("[BtnQA] No button data loaded"); }
        CreatePage();
    }

    public void Hide() { if (pagePanel != null) pagePanel.SetActive(false); }

    void LoadData()
    {
        buttons.Clear();
        TextAsset json = Resources.Load<TextAsset>("ButtonQA_Data");
        if (json == null) { Debug.LogError("[BtnQA] ButtonQA_Data.json not found"); return; }
        Debug.Log("[BtnQA] JSON loaded, " + json.text.Length + " chars");
        string wrapped = json.text.Trim().StartsWith("[") ? "{\"items\":" + json.text + "}" : json.text;
        var w = JsonUtility.FromJson<BtnWrapper>(wrapped);
        if (w == null || w.items == null) { Debug.LogError("[BtnQA] JSON deserialize failed"); return; }
        Debug.Log("[BtnQA] Deserialized " + w.items.Length + " items");
        foreach (var b in w.items)
        {
            if (b != null) buttons.Add(new BtnInfo { name = b.name ?? "???", answer = b.answer ?? "" });
        }
        Debug.Log("[BtnQA] Loaded " + buttons.Count + " buttons");
    }

    void CreatePage()
    {
        Debug.Log("[BtnQA] CreatePage START");
        float s = Mathf.Clamp(Screen.width / 540f, 1f, 2f);

        // Background
        pagePanel = new GameObject("ButtonQAPage", typeof(RectTransform), typeof(Image));
        pagePanel.transform.SetParent(canvas.transform, false);
        pagePanel.transform.SetAsLastSibling();
        pagePanel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        pagePanel.GetComponent<RectTransform>().anchorMax = Vector2.one;
        pagePanel.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        pagePanel.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.12f, 1f);
        Debug.Log("[BtnQA] Background created");

        // Top bar
        GameObject topBar = NewGO("TopBar", pagePanel.transform);
        topBar.AddComponent<Image>().color = new Color(0.03f, 0.06f, 0.14f, 0.93f);
        SetRect(topBar, 1f, 1f, 0.5f, 1f, 0, 40f * s);

        // Back button
        GameObject backBtn = NewGO("BackBtn", topBar.transform);
        backBtn.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.35f, 0.9f);
        backBtn.AddComponent<Button>().onClick.AddListener(() => OnBackToMenu?.Invoke());
        SetRect(backBtn, 0, 0.5f, 0, 0.5f, 8f * s, 0, 38f * s, 28f * s);
        GameObject bl = NewGO("Lbl", backBtn.transform);
        TextMeshProUGUI blt = bl.AddComponent<TextMeshProUGUI>();
        blt.text = "<"; blt.fontSize = 14f * s; blt.alignment = TextAlignmentOptions.Center;
        blt.color = new Color(0.75f, 0.80f, 0.95f, 1f);
        if (menuFont != null) blt.font = menuFont;
        SetRect(bl, 0, 1, 0, 1);

        // Title
        GameObject title = NewGO("Title", topBar.transform);
        TextMeshProUGUI tt = title.AddComponent<TextMeshProUGUI>();
        tt.text = "Button Catalog"; tt.fontSize = 14f * s; tt.alignment = TextAlignmentOptions.Center;
        tt.color = new Color(0.35f, 0.70f, 1f, 1f); tt.fontStyle = FontStyles.Bold;
        if (menuFont != null) tt.font = menuFont;
        SetRect(title, 0.5f, 0.5f, 0.5f, 0.5f, 0, -20f * s, 200f * s, 28f * s);

        // Status line
        GameObject statusObj = NewGO("Status", pagePanel.transform);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = buttons.Count + " buttons loaded. Tap one to see details.";
        statusText.fontSize = 10f * s; statusText.color = new Color(0.4f, 0.45f, 0.55f, 1f);
        statusText.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) statusText.font = menuFont;
        SetRect(statusObj, 0.5f, 1f, 0.5f, 1f, 0, -48f * s, 320f * s, 22f * s);

        // Button list - simple vertical layout, NO ScrollRect
        GameObject listContainer = NewGO("ListContainer", pagePanel.transform);
        SetRect(listContainer, 0.5f, 0.5f, 0.5f, 0.5f, 0, -20f * s, 320f * s, buttons.Count * 52f * s + 10f * s);

        Color[] colors = {
            new Color(0.15f, 0.60f, 0.25f, 0.92f), new Color(0.18f, 0.45f, 0.70f, 0.92f),
            new Color(0.20f, 0.70f, 0.30f, 0.92f), new Color(0.80f, 0.25f, 0.15f, 0.92f),
            new Color(0.85f, 0.55f, 0.10f, 0.92f), new Color(0.60f, 0.60f, 0.10f, 0.92f),
            new Color(0.15f, 0.65f, 0.30f, 0.92f), new Color(0.75f, 0.20f, 0.10f, 0.92f),
            new Color(0.15f, 0.55f, 0.25f, 0.92f)
        };

        for (int i = 0; i < buttons.Count; i++)
        {
            int idx = i;
            var info = buttons[i];
            float y = -i * 52f * s;

            GameObject card = NewGO("Card_" + i, listContainer.transform);
            card.AddComponent<Image>().color = i < colors.Length ? colors[i] : Color.gray;
            card.AddComponent<Button>().onClick.AddListener(() => ShowDetail(idx));
            SetRect(card, 0.5f, 1f, 0.5f, 1f, 0, y, 300f * s, 46f * s);

            GameObject label = NewGO("Lbl", card.transform);
            TextMeshProUGUI lt = label.AddComponent<TextMeshProUGUI>();
            lt.text = (i + 1) + ". " + info.name;
            lt.fontSize = 14f * s; lt.color = Color.white; lt.alignment = TextAlignmentOptions.Center;
            lt.fontStyle = FontStyles.Bold;
            if (menuFont != null) lt.font = menuFont;
            SetRect(label, 0, 1, 0, 1);
        }

        // Detail panel
        detailPanel = NewGO("DetailPanel", pagePanel.transform);
        detailPanel.AddComponent<Image>().color = new Color(0.03f, 0.06f, 0.16f, 0.97f);
        detailPanel.transform.SetAsLastSibling();
        SetRect(detailPanel, 0.05f, 0.92f, 0.05f, 0.08f);

        // Close button
        GameObject closeBtn = NewGO("CloseBtn", detailPanel.transform);
        closeBtn.AddComponent<Image>().color = new Color(0.3f, 0.1f, 0.1f, 0.9f);
        closeBtn.AddComponent<Button>().onClick.AddListener(() => detailPanel.SetActive(false));
        SetRect(closeBtn, 1f, 1f, 1f, 1f, -42f * s, -12f * s, 32f * s, 32f * s);
        GameObject cl = NewGO("Lbl", closeBtn.transform);
        TextMeshProUGUI clt = cl.AddComponent<TextMeshProUGUI>();
        clt.text = "X"; clt.fontSize = 16f * s; clt.alignment = TextAlignmentOptions.Center;
        clt.color = Color.white;
        if (menuFont != null) clt.font = menuFont;
        SetRect(cl, 0, 1, 0, 1);

        // Detail text
        GameObject dtObj = NewGO("DetailText", detailPanel.transform);
        detailText = dtObj.AddComponent<TextMeshProUGUI>();
        detailText.fontSize = 13f * s; detailText.color = new Color(0.88f, 0.9f, 0.95f, 1f);
        detailText.alignment = TextAlignmentOptions.TopLeft;
        if (menuFont != null) detailText.font = menuFont;
        SetRect(dtObj, 0, 1, 0, 1, 16f * s, 16f * s, -16f * s, -60f * s);

        detailPanel.SetActive(false);
        Debug.Log("[BtnQA] CreatePage DONE - " + buttons.Count + " buttons");
    }

    void ShowDetail(int index)
    {
        if (detailText == null || detailPanel == null) return;
        if (index < 0 || index >= buttons.Count) return;
        var info = buttons[index];
        if (info == null) return;
        detailText.text = info.answer ?? "(no info)";
        detailPanel.SetActive(true);
    }

    // ---- Helpers ----
    GameObject NewGO(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    void SetRect(GameObject go, float ax, float ay, float px, float py, float x, float y, float w, float h)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(ax, ay); r.anchorMax = new Vector2(px, py);
        r.pivot = new Vector2(ax, ay);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    void SetRect(GameObject go, float ax, float ay, float px, float py)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(ax, ay); r.anchorMax = new Vector2(px, py);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    void SetRect(GameObject go, float ax, float ay, float px, float py, float l, float t, float r, float b)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(px, py);
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
    }
}