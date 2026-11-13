using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreparationController : MonoBehaviour
{
    [Header("상점 화면")]
    [SerializeField] private Transform ingredientList;
    [SerializeField] private IngredientSelectionCardUI cardPrefab;
    [SerializeField] private TMP_Text selectionCountText;
    [SerializeField] private TMP_Text guideText;
    [SerializeField] private Button departButton;

    [Header("기본 재료 5종, 졸음가루, 진액, 가시, 비늘 순서")]
    [SerializeField] private Sprite[] ingredientSprites = new Sprite[IngredientCatalog.Count];

    private readonly IngredientSelectionCardUI[] cards =
        new IngredientSelectionCardUI[PreparationMarket.IngredientCount];

    private PreparationShopData shop;
    private TMP_Text marketNewsText;
    private TMP_Text statusText;
    private bool isDeparting;

    private void Start()
    {
        PreparationShopData savedShop = GameSession.CurrentSave?.preparationShop;
        shop = PreparationMarket.Upgrade(savedShop)
            ?? PreparationMarket.Create(System.Guid.NewGuid().GetHashCode());

        departButton.onClick.AddListener(Depart);
        CreateMarketUI();
        CreateCards();
        RefreshUI();

        if (!SaveShopState())
        {
            ShowError("상점 정보를 저장하지 못했습니다. Console을 확인해주세요.");
        }
    }

    // 기존 글꼴과 카드 배치를 활용해 소식과 구매 안내를 표시한다.
    private void CreateMarketUI()
    {
        GameObject newsPanel = new GameObject(
            "MarketNewsPanel", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image)
        );
        newsPanel.layer = guideText.gameObject.layer;
        newsPanel.transform.SetParent(guideText.transform.parent, false);

        RectTransform panelRect = newsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -240f);
        panelRect.sizeDelta = new Vector2(1440f, 110f);

        Image panelImage = newsPanel.GetComponent<Image>();
        panelImage.color = new Color(0.20f, 0.23f, 0.20f, 0.96f);
        panelImage.raycastTarget = false;

        marketNewsText = Instantiate(guideText, panelRect);
        marketNewsText.name = "MarketNewsText";
        marketNewsText.fontSize = 22f;
        marketNewsText.enableAutoSizing = false;
        marketNewsText.richText = true;
        marketNewsText.alignment = TextAlignmentOptions.MidlineLeft;
        marketNewsText.raycastTarget = false;
        marketNewsText.margin = Vector4.zero;
        marketNewsText.lineSpacing = 2f;

        RectTransform newsRect = marketNewsText.rectTransform;
        newsRect.anchorMin = Vector2.zero;
        newsRect.anchorMax = Vector2.one;
        newsRect.offsetMin = new Vector2(18f, 10f);
        newsRect.offsetMax = new Vector2(-18f, -10f);
        newsRect.localScale = Vector3.one;

        statusText = Instantiate(guideText, guideText.transform.parent);
        statusText.name = "MarketStatusText";
        statusText.fontSize = 22f;
        statusText.enableAutoSizing = false;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.raycastTarget = false;
        statusText.margin = Vector4.zero;

        RectTransform statusRect = statusText.rectTransform;
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 120f);
        statusRect.sizeDelta = new Vector2(1440f, 32f);
        statusRect.localScale = Vector3.one;
    }

    private void CreateCards()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i] = Instantiate(cardPrefab, ingredientList);
            Sprite icon = i < ingredientSprites.Length ? ingredientSprites[i] : null;
            cards[i].Initialize(
                i, PreparationMarket.GetDisplayName(i), icon, ChangeCount
            );
        }
    }

    private void ChangeCount(int index, int change)
    {
        if (isDeparting || !PreparationMarket.TryChangeQuantity(shop, index, change))
        {
            return;
        }

        if (!SaveShopState())
        {
            PreparationMarket.TryChangeQuantity(shop, index, -change);
            RefreshUI();
            ShowError("장바구니를 저장하지 못했습니다. 다시 시도해주세요.");
            return;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        int totalCost = PreparationMarket.GetTotalCost(shop);
        int totalCount = PreparationMarket.GetTotalCount(shop);
        int remainingCoins = shop.budget - totalCost;

        selectionCountText.text = "남은 코인 " + remainingCoins
            + " / " + shop.budget + "   |   장바구니 " + totalCount + "개";

        marketNewsText.text = "<b>오늘의 숲 소식</b>\n"
            + "<color=#BDE5A8>" + shop.harvestNews + "</color>\n"
            + "<color=#F2B7A0>" + shop.shortageNews + "</color>";

        for (int i = 0; i < cards.Length; i++)
        {
            int stock = shop.stocks[i] - shop.cartCounts[i];
            bool canAdd = stock > 0 && remainingCoins >= shop.prices[i];
            cards[i].UpdateMarketDisplay(
                shop.cartCounts[i], shop.prices[i], stock, canAdd, !isDeparting
            );
        }

        bool hasRecipe = HasRecipe(false);
        departButton.interactable = !isDeparting
            && totalCount >= PreparationMarket.MinimumItemCount && hasRecipe;

        statusText.color = Color.white;

        if (totalCount < PreparationMarket.MinimumItemCount)
        {
            statusText.text = "첫 손패를 채울 수 있도록 재료를 최소 "
                + PreparationMarket.MinimumItemCount + "개 담아주세요.";
        }
        else if (!hasRecipe)
        {
            statusText.text = "물약을 제조할 수 있는 재료 조합을 함께 담아주세요.";
        }
        else if (!HasRecipe(true))
        {
            statusText.text = "공격 물약을 만들 재료도 챙기면 탐사에 도움이 됩니다.";
            statusText.color = new Color(1f, 0.82f, 0.45f);
        }
        else
        {
            statusText.text = "출발하면 장바구니 재료를 구매하고 탐사를 시작합니다.";
        }
    }

    private List<string> GetCartIngredients()
    {
        List<string> ingredients = new List<string>();

        for (int i = 0; i < PreparationMarket.IngredientCount; i++)
        {
            for (int count = 0; count < shop.cartCounts[i]; count++)
            {
                ingredients.Add(PreparationMarket.GetIngredientName(i));
            }
        }

        return ingredients;
    }

    // 현재 등록된 실제 레시피를 기준으로 2개와 3개 재료 조합을 확인한다.
    private bool HasRecipe(bool requireDamage)
    {
        List<string> ingredients = GetCartIngredients();

        for (int i = 0; i < ingredients.Count; i++)
        {
            for (int j = i + 1; j < ingredients.Count; j++)
            {
                PotionData pair = PotionRecipes.Create(
                    new List<string> { ingredients[i], ingredients[j] }
                );
                if (pair != null && (!requireDamage || pair.damage > 0))
                {
                    return true;
                }

                for (int k = j + 1; k < ingredients.Count; k++)
                {
                    PotionData triple = PotionRecipes.Create(
                        new List<string> { ingredients[i], ingredients[j], ingredients[k] }
                    );
                    if (triple != null && (!requireDamage || triple.damage > 0))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // 장바구니와 소식도 저장해 이어하기로 가격을 다시 뽑지 못하게 한다.
    private bool SaveShopState()
    {
        int slotNumber = GameSession.SlotNumber;
        GameSaveData currentSave = GameSession.CurrentSave;

        if (slotNumber < 1 || slotNumber > 3 || currentSave == null)
        {
            return true;
        }

        GameSaveData updatedSave = JsonUtility.FromJson<GameSaveData>(
            JsonUtility.ToJson(currentSave)
        );
        updatedSave.sceneName = "Preparation";
        updatedSave.hasBattleState = false;
        updatedSave.coins = shop.budget;
        updatedSave.preparationShop = PreparationMarket.Copy(shop);

        if (!SaveSystem.Save(slotNumber, updatedSave))
        {
            return false;
        }

        GameSession.SelectSave(slotNumber, updatedSave);
        return true;
    }

    private void Depart()
    {
        if (isDeparting
            || PreparationMarket.GetTotalCount(shop) < PreparationMarket.MinimumItemCount
            || !HasRecipe(false))
        {
            return;
        }

        int slotNumber = GameSession.SlotNumber;
        GameSaveData currentSave = GameSession.CurrentSave;

        if (slotNumber < 1 || slotNumber > 3 || currentSave == null)
        {
            ShowError("출발 테스트는 Title 씬에서 저장 슬롯을 선택한 뒤 진행해주세요.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded("BattleTest"))
        {
            ShowError("Scene List에 BattleTest 씬을 추가해주세요.");
            return;
        }

        GameSaveData updatedSave = JsonUtility.FromJson<GameSaveData>(
            JsonUtility.ToJson(currentSave)
        );
        updatedSave.bagTokens = GetCartIngredients().ToArray();
        updatedSave.preparationShop = PreparationMarket.AfterPurchase(shop);
        updatedSave.coins = updatedSave.preparationShop.budget;
        updatedSave.sceneName = "BattleTest";
        updatedSave.hasSeenPrologue = true;
        updatedSave.hasBattleState = false;

        if (!SaveSystem.Save(slotNumber, updatedSave))
        {
            ShowError("구매 내용을 저장하지 못했습니다. 다시 시도해주세요.");
            return;
        }

        GameSession.SelectSave(slotNumber, updatedSave);
        isDeparting = true;
        RefreshUI();
        if (!SceneTransition.LoadScene("BattleTest", CancelDeparture))
            CancelDeparture();
    }

    private void ShowError(string message)
    {
        statusText.text = message;
        statusText.color = new Color(1f, 0.65f, 0.55f);
    }

    private void CancelDeparture()
    {
        isDeparting = false;
        RefreshUI();
        ShowError("전투 화면으로 이동하지 못했습니다. 다시 시도해주세요.");
    }

}
