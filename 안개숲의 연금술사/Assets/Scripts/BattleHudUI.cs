using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleHudUI : MonoBehaviour
{
    private static readonly Color BarColor = new Color32(22, 29, 36, 245);
    private static readonly Color PanelColor = new Color32(29, 37, 44, 255);
    private static readonly Color Gold = new Color32(220, 188, 128, 255);
    private static readonly Color Ink = new Color32(246, 240, 223, 255);
    private static readonly Color Muted = new Color32(196, 201, 199, 255);

    private BagController battle;
    private TMP_FontAsset font;
    private string area;
    private TMP_Text healthText;
    private TMP_Text progressText;
    private CanvasGroup progress;
    private float progressUntil;
    private Button mapButton;
    private Button pouchButton;
    private Button codexButton;
    private Button settingsButton;
    private RectTransform modal;
    private TMP_Text modalTitle;
    private Button closeButton;
    private RectTransform mapPage;
    private RectTransform pouchPage;
    private RectTransform codexPage;
    private ScrollRect codexScroll;
    private TMP_Text codexText;
    private TMP_Text basicTab;
    private TMP_Text additiveTab;
    private string basicRecipes;
    private string additiveRecipes;

    public TMP_Text TurnText { get; private set; }
    public bool IsPanelOpen => modal != null && modal.gameObject.activeSelf;

    public static BattleHudUI Create(Transform parent, TMP_FontAsset font, Sprite portrait,
        BagController battle, PauseMenuController pauseMenu, string area)
    {
        RectTransform root = Rect("BattleHud", parent);
        Place(root, 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);
        BattleHudUI hud = root.gameObject.AddComponent<BattleHudUI>();
        hud.battle = battle;
        hud.font = font;
        hud.area = area;
        hud.Build(portrait);
        if (pauseMenu != null) pauseMenu.UseSettingsButton(hud.settingsButton);
        else hud.settingsButton.interactable = false;
        return hud;
    }

    private void Build(Sprite portrait)
    {
        Image bar = Panel("TopBar", transform, BarColor);
        Place(bar.rectTransform, 0f, 1f, 1f, 1f, new Vector2(0, -66), Vector2.zero);
        Image line = Panel("GoldRule", bar.transform, Gold);
        Place(line.rectTransform, 0f, 0f, 1f, 0f, Vector2.zero, new Vector2(0, 2));

        Image portraitImage = Panel("Portrait", bar.transform, Color.white);
        portraitImage.sprite = portrait;
        portraitImage.enabled = portrait != null;
        portraitImage.preserveAspect = true;
        Place(portraitImage.rectTransform, 0f, 0f, 0f, 1f, new Vector2(20, 7), new Vector2(66, -7));
        healthText = Label("PlayerStatus", bar.transform, "", 26, Ink, TextAlignmentOptions.Left);
        Place(healthText.rectTransform, 0f, 0f, .30f, 1f, new Vector2(76, 4), new Vector2(-8, -4));

        TMP_Text location = Label("CurrentArea", bar.transform, area, 24, Gold);
        Place(location.rectTransform, .30f, 0f, .465f, 1f, new Vector2(6, 4), new Vector2(-4, -4));
        TurnText = Label("CurrentTurn", bar.transform, "", 24, Ink);
        Place(TurnText.rectTransform, .465f, 0f, .62f, 1f, new Vector2(4, 4), new Vector2(-6, -4));

        mapButton = MenuButton(bar.transform, "지도", .62f, .702f, OpenMap, out _);
        pouchButton = MenuButton(bar.transform, "주머니", .702f, .812f, OpenPouch, out TMP_Text pouchText);
        pouchText.text = $"주머니 {battle.PouchIngredients.Count}";
        codexButton = MenuButton(bar.transform, "도감", .812f, .902f, OpenCodex, out _);
        settingsButton = MenuButton(bar.transform, "설정", .902f, 1f, null, out _);

        Image relicPlate = Panel("Relics", transform, BarColor);
        Place(relicPlate.rectTransform, 0f, 1f, .30f, 1f, new Vector2(20, -111), new Vector2(-8, -73));
        TMP_Text relicText = Label("RelicStatus", relicPlate.transform, "유물 없음", 21, Muted, TextAlignmentOptions.Left);
        Place(relicText.rectTransform, 0f, 0f, 1f, 1f, new Vector2(14, 2), new Vector2(-14, -2));

        Image progressPlate = Panel("BattleProgress", transform, BarColor);
        Place(progressPlate.rectTransform, .31f, 1f, .69f, 1f, new Vector2(0, -125), new Vector2(0, -76));
        progress = progressPlate.gameObject.AddComponent<CanvasGroup>();
        progress.interactable = false;
        progress.blocksRaycasts = false;
        progressText = Label("ProgressText", progressPlate.transform, "", 25, Ink);
        Place(progressText.rectTransform, 0f, 0f, 1f, 1f, new Vector2(14, 2), new Vector2(-14, -2));
        ClearProgress();
        BuildModal();
    }

    public void SetPlayerStatus(int hp, int maxHp, int armor, bool afterheat)
    {
        healthText.text = $"연금술사    체력 {hp} / {maxHp}    방어도 {armor}"
            + (afterheat ? "    잔열" : "");
    }

    public void ShowProgress(string message)
    {
        progressText.text = message;
        progress.alpha = 1f;
        progressUntil = Time.unscaledTime + 1.6f;
    }

    public void ClearProgress()
    {
        progressUntil = 0f;
        progress.alpha = 0f;
    }

    private void LateUpdate()
    {
        bool available = battle.CanOpenBattleMenu();
        mapButton.interactable = available;
        pouchButton.interactable = available;
        codexButton.interactable = available;
        if (progressUntil > 0f)
        {
            float remaining = progressUntil - Time.unscaledTime;
            progress.alpha = Mathf.Clamp01(remaining / .25f);
            if (remaining <= 0f) progressUntil = 0f;
        }
    }

    private void BuildModal()
    {
        Image shade = Panel("MenuOverlay", transform, new Color32(3, 8, 11, 208));
        shade.raycastTarget = true;
        modal = shade.rectTransform;
        Place(modal, 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);
        Image window = Panel("MenuWindow", modal, PanelColor);
        Place(window.rectTransform, .15f, .13f, .85f, .87f, Vector2.zero, Vector2.zero);
        Image rule = Panel("WindowRule", window.transform, Gold);
        Place(rule.rectTransform, 0f, 1f, 1f, 1f, new Vector2(0, -3), Vector2.zero);
        modalTitle = Label("MenuTitle", window.transform, "", 38, Gold, TextAlignmentOptions.Left);
        Place(modalTitle.rectTransform, 0f, 1f, 1f, 1f, new Vector2(34, -81), new Vector2(-178, -14));
        closeButton = ButtonWithLabel(window.transform, "닫기", ClosePanel, out _);
        Place((RectTransform)closeButton.transform, 1f, 1f, 1f, 1f, new Vector2(-152, -70), new Vector2(-26, -19));

        mapPage = Rect("MapPage", window.transform);
        Place(mapPage, 0f, 0f, 1f, 1f, new Vector2(34, 32), new Vector2(-34, -95));
        Image locationCard = Panel("MapLocation", mapPage, BarColor);
        Place(locationCard.rectTransform, .18f, .30f, .82f, .70f, Vector2.zero, Vector2.zero);
        TMP_Text location = Label("LocationMarker", locationCard.transform, "현재 위치\n" + area, 40, Ink);
        Place(location.rectTransform, 0f, 0f, 1f, 1f, new Vector2(15, 8), new Vector2(-15, -8));

        pouchPage = Rect("PouchPage", window.transform);
        Place(pouchPage, 0f, 0f, 1f, 1f, new Vector2(34, 28), new Vector2(-34, -95));

        codexPage = Rect("CodexPage", window.transform);
        Place(codexPage, 0f, 0f, 1f, 1f, new Vector2(34, 28), new Vector2(-34, -95));
        Button basic = ButtonWithLabel(codexPage, "기본 조합", () => ShowRecipes(false), out basicTab);
        Place((RectTransform)basic.transform, 0f, 1f, 0f, 1f, new Vector2(0, -52), new Vector2(210, 0));
        Button additive = ButtonWithLabel(codexPage, "첨가제 조합", () => ShowRecipes(true), out additiveTab);
        Place((RectTransform)additive.transform, 0f, 1f, 0f, 1f, new Vector2(226, -52), new Vector2(450, 0));

        Image scrollPlate = Panel("RecipeScroll", codexPage, BarColor);
        scrollPlate.raycastTarget = true;
        Place(scrollPlate.rectTransform, 0f, 0f, 1f, 1f, Vector2.zero, new Vector2(0, -68));
        codexScroll = scrollPlate.gameObject.AddComponent<ScrollRect>();
        RectTransform viewport = Rect("Viewport", scrollPlate.transform);
        viewport.gameObject.AddComponent<RectMask2D>();
        Place(viewport, 0f, 0f, 1f, 1f, new Vector2(26, 12), new Vector2(-26, -12));
        codexText = Label("Recipes", viewport, "", 26, Ink, TextAlignmentOptions.TopLeft);
        codexText.enableAutoSizing = false;
        codexText.textWrappingMode = TextWrappingModes.Normal;
        RectTransform content = codexText.rectTransform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        codexScroll.viewport = viewport;
        codexScroll.content = content;
        codexScroll.horizontal = false;
        codexScroll.movementType = ScrollRect.MovementType.Clamped;
        codexScroll.scrollSensitivity = 42f;
        Image track = Panel("ScrollTrack", scrollPlate.transform, PanelColor);
        track.raycastTarget = true;
        Place(track.rectTransform, 1f, 0f, 1f, 1f, new Vector2(-18, 12), new Vector2(-7, -12));
        Image thumb = Panel("ScrollThumb", track.transform, Gold);
        thumb.raycastTarget = true;
        Place(thumb.rectTransform, 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = thumb.rectTransform;
        scrollbar.targetGraphic = thumb;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        codexScroll.verticalScrollbar = scrollbar;
        codexScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        modal.gameObject.SetActive(false);
    }

    private void OpenMap()
    {
        if (!battle.CanOpenBattleMenu()) return;
        OpenPanel("지도", mapPage);
    }

    private void OpenCodex()
    {
        if (!battle.CanOpenBattleMenu()) return;
        OpenPanel("물약 도감", codexPage);
        ShowRecipes(false);
    }

    private void OpenPouch()
    {
        if (!battle.CanOpenBattleMenu()) return;

        IReadOnlyList<string> ingredients = battle.PouchIngredients;
        foreach (Transform child in pouchPage)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        Dictionary<string, int> counts = CountPouchIngredients(ingredients);
        if (counts.Count == 0)
        {
            TMP_Text empty = Label("EmptyPouch", pouchPage, "보유한 재료가 없습니다.", 28, Muted);
            Place(empty.rectTransform, 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);
        }

        const int columns = 3;
        int rows = Mathf.Max(3, Mathf.CeilToInt((float)counts.Count / columns));
        int index = 0;
        foreach (KeyValuePair<string, int> entry in counts)
        {
            int column = index % columns;
            int row = index / columns;
            Image tile = Panel("Ingredient", pouchPage, BarColor);
            Place(tile.rectTransform, (float)column / columns, 1f - (float)(row + 1) / rows,
                (float)(column + 1) / columns, 1f - (float)row / rows,
                new Vector2(6, 6), new Vector2(-6, -6));

            Image icon = Panel("IngredientIcon", tile.transform, Color.white);
            icon.sprite = battle.GetTokenSprite(entry.Key);
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;
            Place(icon.rectTransform, 0f, .14f, .30f, .86f, new Vector2(10, 0), new Vector2(-6, 0));

            TMP_Text name = Label("IngredientName", tile.transform, IngredientCatalog.DisplayName(entry.Key),
                29, Ink, TextAlignmentOptions.Left);
            Place(name.rectTransform, .33f, .43f, 1f, .90f, Vector2.zero, new Vector2(-10, 0));
            TMP_Text amount = Label("IngredientCount", tile.transform, $"× {entry.Value}",
                25, Gold, TextAlignmentOptions.Left);
            Place(amount.rectTransform, .33f, .10f, 1f, .43f, Vector2.zero, new Vector2(-10, 0));
            index++;
        }

        OpenPanel($"주머니 · 전체 재료 {ingredients.Count}개", pouchPage);
    }

    private static Dictionary<string, int> CountPouchIngredients(IReadOnlyList<string> ingredients)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (string ingredient in ingredients)
        {
            string name = IngredientCatalog.Normalize(ingredient);
            if (string.IsNullOrEmpty(name)) continue;
            counts.TryGetValue(name, out int count);
            counts[name] = count + 1;
        }
        return counts;
    }

    private void OpenPanel(string title, RectTransform page)
    {
        modalTitle.text = title;
        mapPage.gameObject.SetActive(page == mapPage);
        pouchPage.gameObject.SetActive(page == pouchPage);
        codexPage.gameObject.SetActive(page == codexPage);
        transform.SetAsLastSibling();
        modal.gameObject.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(closeButton.gameObject);
    }

    public void ClosePanel()
    {
        if (!IsPanelOpen) return;
        modal.gameObject.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void ShowRecipes(bool withAdditives)
    {
        if (withAdditives)
            codexText.text = additiveRecipes ?? (additiveRecipes = BuildRecipeList(true));
        else
            codexText.text = basicRecipes ?? (basicRecipes = BuildRecipeList(false));
        basicTab.color = withAdditives ? Ink : Gold;
        additiveTab.color = withAdditives ? Gold : Ink;
        codexScroll.StopMovement();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(codexText.rectTransform);
        codexScroll.verticalNormalizedPosition = 1f;
    }

    private static string BuildRecipeList(bool withAdditives)
    {
        StringBuilder text = new StringBuilder();
        string[] bases = IngredientCatalog.BaseIngredients;
        for (int i = 0; i < bases.Length; i++)
        {
            for (int j = i; j < bases.Length; j++)
            {
                List<string> ingredients = new List<string> { bases[i], bases[j] };
                if (!withAdditives)
                {
                    AppendRecipe(text, ingredients);
                    continue;
                }
                foreach (string additive in IngredientCatalog.Additives)
                {
                    ingredients.Add(additive);
                    AppendRecipe(text, ingredients);
                    ingredients.RemoveAt(2);
                }
            }
        }
        return text.ToString();
    }

    private static void AppendRecipe(StringBuilder text, List<string> ingredients)
    {
        PotionData potion = PotionRecipes.Create(ingredients);
        if (potion == null) return;
        text.Append("<color=#DCBC80><b>");
        for (int i = 0; i < ingredients.Count; i++)
        {
            if (i > 0) text.Append(" + ");
            text.Append(IngredientCatalog.DisplayName(ingredients[i]));
        }
        text.AppendLine("</b></color>");
        text.AppendLine(PotionEffects.Describe(potion)).AppendLine();
    }

    private Button MenuButton(Transform parent, string caption, float left, float right,
        UnityEngine.Events.UnityAction action, out TMP_Text label)
    {
        Button button = ButtonWithLabel(parent, caption, action, out label);
        Place((RectTransform)button.transform, left, 0f, right, 1f, new Vector2(4, 8), new Vector2(-6, -8));
        return button;
    }

    private Button ButtonWithLabel(Transform parent, string caption, UnityEngine.Events.UnityAction action,
        out TMP_Text label)
    {
        Image image = Panel(caption + "Button", parent, new Color32(65, 76, 85, 255));
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        if (action != null) button.onClick.AddListener(action);
        label = Label("Label", image.transform, caption, 25, Ink);
        Place(label.rectTransform, 0f, 0f, 1f, 1f, new Vector2(5, 2), new Vector2(-5, -2));
        return button;
    }

    private TMP_Text Label(string name, Transform parent, string text, float size, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        RectTransform rect = Rect(name, parent);
        TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.fontSizeMin = Mathf.Min(17f, size);
        label.fontSizeMax = size;
        label.enableAutoSizing = true;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    private static Image Panel(string name, Transform parent, Color color)
    {
        Image image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject item = new GameObject(name, typeof(RectTransform));
        item.layer = parent.gameObject.layer;
        item.transform.SetParent(parent, false);
        return (RectTransform)item.transform;
    }

    private static void Place(RectTransform rect, float left, float bottom, float right, float top,
        Vector2 minimumOffset, Vector2 maximumOffset)
    {
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = minimumOffset;
        rect.offsetMax = maximumOffset;
    }
}
