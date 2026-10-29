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

    private int ingredientIndex;
    private Action<int, int> onCountChanged;

    private void Awake()
    {
        minusButton.onClick.AddListener(() => ChangeCount(-1));
        plusButton.onClick.AddListener(() => ChangeCount(1));
    }

    // 이 카드에 표시할 재료와 버튼 동작을 연결한다.
    public void Initialize(
        int index,
        string displayName,
        Sprite icon,
        Action<int, int> countChanged)
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

    // 선택 개수와 증감 버튼의 사용 가능 상태를 갱신한다.
    public void UpdateDisplay(int amount, bool canAdd)
    {
        amountText.text = amount.ToString();
        minusButton.interactable = amount > 0;
        plusButton.interactable = canAdd;
    }
}