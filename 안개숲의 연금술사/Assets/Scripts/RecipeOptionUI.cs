using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 조합 목록 한 칸의 재료 이미지와 물약 이름을 표시한다.
public class RecipeOptionUI : MonoBehaviour
{
    [SerializeField] private Image ingredientIcon;
    [SerializeField] private TMP_Text potionNameText;

    public void Setup(
        Sprite ingredientSprite,
        string potionName,
        string requirement)
    {
        ingredientIcon.sprite = ingredientSprite;
        ingredientIcon.preserveAspect = true;
        ingredientIcon.enabled = ingredientSprite != null;

        potionNameText.text = potionName;

        if (!string.IsNullOrEmpty(requirement))
        {
            potionNameText.text +=
                "\n<size=18>" + requirement + "</size>";
        }
    }
}