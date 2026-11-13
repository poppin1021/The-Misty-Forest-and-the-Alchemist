using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 썩은별 가시의 선택 추출은 현재 주머니에 남은 재료만 보여준다.
public class TokenChoiceUI : MonoBehaviour
{
    private Transform optionsRoot;
    private TMP_FontAsset font;
    private Action<string> onSelected;

    public static TokenChoiceUI Create(Transform parent, TMP_FontAsset fontAsset)
    {
        GameObject root = Panel("TokenChoicePanel", parent, new Color(0f, 0f, 0f, 0.75f));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

        TokenChoiceUI ui = root.AddComponent<TokenChoiceUI>();
        ui.font = fontAsset;
        ui.BuildWindow();
        root.SetActive(false);
        return ui;
    }

    private void BuildWindow()
    {
        GameObject window = Panel("ChoiceWindow", transform, new Color(0.15f, 0.18f, 0.15f, 1f));
        SetRect(window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1050f, 600f));

        TMP_Text title = Text("ChoiceTitle", window.transform, "추출할 재료 선택", 32f);
        SetRect(title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(950f, 50f));
        TMP_Text guide = Text("ChoiceGuide", window.transform, "주머니에 남아 있는 재료 중 1개를 골라주세요.", 24f);
        SetRect(guide.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(950f, 50f));

        GameObject grid = new GameObject("Choices", typeof(RectTransform), typeof(GridLayoutGroup));
        grid.layer = gameObject.layer;
        grid.transform.SetParent(window.transform, false);
        SetRect(grid.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), new Vector2(960f, 400f));
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(300f, 120f);
        layout.spacing = new Vector2(18f, 18f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        layout.childAlignment = TextAnchor.MiddleCenter;
        optionsRoot = grid.transform;
    }

    public bool Show(IList<string> available, Func<string, Sprite> getSprite, Action<string> selected)
    {
        if (available == null || available.Count == 0) return false;
        foreach (Transform child in optionsRoot)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (string token in available)
        {
            if (!counts.ContainsKey(token)) counts[token] = 0;
            counts[token]++;
        }

        onSelected = selected;
        foreach (KeyValuePair<string, int> entry in counts)
        {
            string tokenName = entry.Key;
            GameObject option = Panel("Choose_" + tokenName, optionsRoot, new Color(0.28f, 0.32f, 0.27f, 1f));
            Button button = option.AddComponent<Button>();
            button.targetGraphic = option.GetComponent<Image>();
            button.onClick.AddListener(() => Select(tokenName));

            GameObject icon = Panel("Icon", option.transform, Color.white);
            SetRect(icon.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-96f, 10f), new Vector2(70f, 70f));
            Image image = icon.GetComponent<Image>();
            image.sprite = getSprite(tokenName);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = image.sprite != null;

            TMP_Text name = Text("Name", option.transform, IngredientCatalog.DisplayName(tokenName), 24f);
            SetRect(name.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(45f, 12f), new Vector2(185f, 68f));
            TMP_Text amount = Text("Amount", option.transform, "남은 수량 " + entry.Value + "개", 20f);
            SetRect(amount.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(280f, 30f));
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(optionsRoot as RectTransform);
        return true;
    }

    private void Select(string tokenName)
    {
        Action<string> selected = onSelected;
        Hide();
        selected?.Invoke(tokenName);
    }

    public void Hide()
    {
        onSelected = null;
        gameObject.SetActive(false);
    }

    private static GameObject Panel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = parent.gameObject.layer;
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private TMP_Text Text(string name, Transform parent, string value, float size)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = value;
        return text;
    }

    private static void SetRect(Transform transform, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform rect = transform as RectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
