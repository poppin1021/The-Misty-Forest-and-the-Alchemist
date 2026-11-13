using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientSelectionCardUI : MonoBehaviour
{
    [Header("카드 구성")]
    [SerializeField] private Image ingredientIcon;
    [SerializeField] private TMP_Text ingredientNameText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;

    private TMP_Text priceStockText;
    private int ingredientIndex;
    private Action<int, int> onCountChanged;

    private void Awake()
    {
        minusButton.onClick.AddListener(() => ChangeCount(-1));
        plusButton.onClick.AddListener(() => ChangeCount(1));
        ConfigureCompactLayout();
        CreatePriceLabel();
    }

    public void Initialize(
        int index, string displayName, Sprite icon, Action<int, int> countChanged)
    {
        ingredientIndex = index;
        onCountChanged = countChanged;
        ingredientNameText.text = displayName;
        ingredientIcon.sprite = icon;
        ingredientIcon.preserveAspect = true;
        ingredientIcon.enabled = icon != null;
        UpdateDisplay(0, true);
    }

    private void ChangeCount(int change)
    {
        onCountChanged?.Invoke(ingredientIndex, change);
    }

    public void UpdateDisplay(int amount, bool canAdd)
    {
        amountText.text = amount.ToString();
        minusButton.interactable = amount > 0;
        plusButton.interactable = canAdd;
    }

    public void UpdateMarketDisplay(
        int amount, int price, int remainingStock, bool canAdd, bool canEdit)
    {
        UpdateDisplay(amount, canEdit && canAdd);
        minusButton.interactable = canEdit && amount > 0;
        priceStockText.text = price + "코인  |  남은 재고 " + remainingStock + "개";
    }

    // 기존 카드 프리팹의 글꼴을 사용해 가격과 재고 표시를 추가한다.
    private void CreatePriceLabel()
    {
        priceStockText = Instantiate(ingredientNameText, transform);
        priceStockText.name = "PriceStockText";
        priceStockText.text = "";
        priceStockText.fontSize = 20f;
        priceStockText.enableAutoSizing = false;
        priceStockText.alignment = TextAlignmentOptions.Center;
        priceStockText.raycastTarget = false;
        priceStockText.margin = Vector4.zero;

        RectTransform rect = priceStockText.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 8f);
        rect.sizeDelta = new Vector2(420f, 28f);
        rect.localScale = Vector3.one;
    }

    // 9종을 3열 3행으로 표시할 수 있도록 카드 내부를 배치한다.
    private void ConfigureCompactLayout()
    {
        RectTransform nameRect = ingredientNameText.rectTransform;
        nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -8f);
        nameRect.sizeDelta = new Vector2(400f, 34f);
        ingredientNameText.fontSize = 24f;
        ingredientNameText.enableAutoSizing = false;
        SetControlRect(ingredientIcon.rectTransform, -125f, 76f, 76f);
        SetControlRect(minusButton.transform as RectTransform, -22f, 56f, 56f);
        SetControlRect(amountText.rectTransform, 58f, 80f, 56f);
        SetControlRect(plusButton.transform as RectTransform, 138f, 56f, 56f);
        amountText.fontSize = 30f;
    }

    private void SetControlRect(RectTransform rect, float x, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, -8f);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }
}
