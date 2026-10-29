using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreparationController : MonoBehaviour
{
    private const int RequiredCount = 10;
    private const int MaxPerIngredient = 3;


    [Header("화면 연결")]
    [SerializeField] private Transform ingredientList;
    [SerializeField] private IngredientSelectionCardUI cardPrefab;
    [SerializeField] private TMP_Text selectionCountText;
    [SerializeField] private Button departButton;

    [Header("홍등열매, 갑각잎, 새살꽃, 시듦버섯, 바람씨앗, 졸음가루 순서")]
    [SerializeField] private Sprite[] ingredientSprites = new Sprite[6];

    // 실제 전투에서 사용하는 재료 이름이다.
    private readonly string[] ingredientNames =
    {
        "홍등열매",
        "갑각잎",
        "새살꽃",
        "시듦버섯",
        "바람씨앗",
        "졸음나방 날개가루"
    };

    private readonly int[] selectedCounts = new int[6];

    private readonly IngredientSelectionCardUI[] cards =
        new IngredientSelectionCardUI[6];

    private int totalCount;
    private bool isDeparting;

    private void Start()
    {
        departButton.onClick.AddListener(Depart);

        CreateCards();
        RefreshUI();
    }

    // 재료 종류별로 카드를 하나씩 생성한다.
    private void CreateCards()
    {
        for (int i = 0; i < ingredientNames.Length; i++)
        {
            IngredientSelectionCardUI card =
                Instantiate(cardPrefab, ingredientList);

            cards[i] = card;

            string displayName =
                ingredientNames[i] == "졸음나방 날개가루"
                    ? "졸음가루"
                    : ingredientNames[i];

            Sprite icon =
                i < ingredientSprites.Length
                    ? ingredientSprites[i]
                    : null;

            card.Initialize(
                i,
                displayName,
                icon,
                ChangeCount
            );
        }
    }

    // 개수가 음수가 되거나 전체 10개를 초과하지 않도록 제한한다.
    private void ChangeCount(int index, int change)
    {
        if (isDeparting)
        {
            return;
        }

        int nextCount = selectedCounts[index] + change;
        int nextTotal = totalCount + change;

        if (nextCount < 0
    || nextCount > MaxPerIngredient
    || nextTotal > RequiredCount)
        {
            return;
        }

        selectedCounts[index] = nextCount;
        totalCount = nextTotal;

        RefreshUI();
    }

    private void RefreshUI()
    {
        selectionCountText.text =
            $"선택한 재료 {totalCount} / {RequiredCount}";

        bool canAdd =
            !isDeparting && totalCount < RequiredCount;

        for (int i = 0; i < cards.Length; i++)
        {
            bool canAddThisIngredient =
                canAdd && selectedCounts[i] < MaxPerIngredient;

            cards[i].UpdateDisplay(
                selectedCounts[i],
                canAddThisIngredient
            );
        }

        departButton.interactable =
            !isDeparting && totalCount == RequiredCount;
    }

    // 선택한 재료를 현재 슬롯에 저장한 뒤 첫 전투로 이동한다.
    private void Depart()
    {
        if (isDeparting || totalCount != RequiredCount)
        {
            return;
        }

        int slotNumber = GameSession.SlotNumber;
        GameSaveData currentSave = GameSession.CurrentSave;

        if (slotNumber < 1 || slotNumber > 3 || currentSave == null)
        {
            Debug.LogError(
                "출발 테스트는 Title 씬에서 저장 슬롯을 선택한 뒤 진행해주세요."
            );
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded("BattleTest"))
        {
            Debug.LogError(
                "Build Profiles의 Scene List에 BattleTest 씬을 추가해주세요."
            );
            return;
        }

        List<string> selectedIngredients = new List<string>();

        for (int i = 0; i < ingredientNames.Length; i++)
        {
            for (int count = 0; count < selectedCounts[i]; count++)
            {
                selectedIngredients.Add(ingredientNames[i]);
            }
        }

        // 저장 실패 시 기존 진행 정보가 바뀌지 않도록 복사한다.
        GameSaveData updatedSave =
            JsonUtility.FromJson<GameSaveData>(
                JsonUtility.ToJson(currentSave)
            );

        updatedSave.bagTokens = selectedIngredients.ToArray();
        updatedSave.sceneName = "BattleTest";
        updatedSave.hasSeenPrologue = true;
        updatedSave.hasBattleState = false;

        if (!SaveSystem.Save(slotNumber, updatedSave))
        {
            return;
        }

        GameSession.SelectSave(slotNumber, updatedSave);

        isDeparting = true;
        RefreshUI();

        SceneManager.LoadScene("BattleTest");
    }
}