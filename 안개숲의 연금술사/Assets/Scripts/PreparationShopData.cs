using System;

// 같은 탐사에서 가격, 재고, 장바구니와 소식을 유지하기 위한 저장 데이터다.
[Serializable]
public class PreparationShopData
{
    public int budget = 100;
    public int[] prices = new int[IngredientCatalog.Count];
    public int[] stocks = new int[IngredientCatalog.Count];
    public int[] cartCounts = new int[IngredientCatalog.Count];
    public string harvestNews = "";
    public string shortageNews = "";
}

// 상점의 가격 생성과 구매 계산은 화면 코드와 분리한다.
public static class PreparationMarket
{
    public const int StartingCoins = 100;
    public const int IngredientCount = IngredientCatalog.Count;
    public const int MinimumItemCount = 5;

    private static readonly int[] BasePrices = { 12, 11, 11, 9, 7, 18, 17, 16, 16 };

    private static readonly string[] IngredientNames =
    {
        "홍등열매", "갑각잎", "새살꽃", "시듦버섯", "바람씨앗",
        "졸음나방 날개가루", "허물달팽이 진액", "썩은별 가시", "불씨도롱뇽 비늘"
    };

    private static readonly string[] HarvestHeadlines =
    {
        "홍등열매가 풍년입니다.",
        "갑각잎 수확량이 늘었습니다.",
        "새살꽃이 많이 피었습니다.",
        "시듦버섯 채집이 순조롭습니다.",
        "바람씨앗이 많이 모였습니다.",
        "졸음나방이 많이 나타났습니다.",
        "허물달팽이 진액이 많이 모였습니다.",
        "썩은별 가시 수확량이 늘었습니다.",
        "불씨도롱뇽 비늘 공급이 늘었습니다."
    };

    private static readonly string[] ShortageHeadlines =
    {
        "홍등열매 수확이 줄었습니다.",
        "갑각잎 채집이 어려워졌습니다.",
        "새살꽃 수확이 줄었습니다.",
        "시듦버섯 공급이 줄었습니다.",
        "바람씨앗을 모으기 어렵습니다.",
        "졸음가루 운송이 늦어졌습니다.",
        "허물달팽이 진액 공급이 줄었습니다.",
        "썩은별 가시 채집이 어려워졌습니다.",
        "불씨도롱뇽 비늘 운송이 늦어졌습니다."
    };

    public static string GetIngredientName(int index)
    {
        return IngredientNames[index];
    }

    public static string GetDisplayName(int index)
    {
        return IngredientCatalog.DisplayName(IngredientNames[index]);
    }

    // 새 탐사에서만 풍년과 공급 부족 소식을 한 가지씩 결정한다.
    public static PreparationShopData Create(int seed)
    {
        Random random = new Random(seed);
        PreparationShopData data = new PreparationShopData();

        for (int i = 0; i < IngredientCount; i++)
        {
            data.prices[i] = BasePrices[i] + random.Next(-1, 2);
            data.stocks[i] = i == 0 ? random.Next(3, 5) : random.Next(2, 5);
        }

        int harvestIndex = random.Next(IngredientCount);
        int shortageIndex = random.Next(IngredientCount - 1);

        if (shortageIndex >= harvestIndex)
        {
            shortageIndex++;
        }

        int oldPrice = data.prices[harvestIndex];
        int oldStock = data.stocks[harvestIndex];

        data.prices[harvestIndex] = Math.Max(
            1, (int)Math.Round(oldPrice * 0.8, MidpointRounding.AwayFromZero)
        );
        data.stocks[harvestIndex] = oldStock + 2;

        data.harvestNews = BuildNews(
            HarvestHeadlines[harvestIndex], oldPrice, data.prices[harvestIndex],
            oldStock, data.stocks[harvestIndex]
        );

        oldPrice = data.prices[shortageIndex];
        oldStock = data.stocks[shortageIndex];
        data.prices[shortageIndex] = (int)Math.Ceiling(oldPrice * 1.25);
        data.stocks[shortageIndex] = oldStock - 1;

        data.shortageNews = BuildNews(
            ShortageHeadlines[shortageIndex], oldPrice, data.prices[shortageIndex],
            oldStock, data.stocks[shortageIndex]
        );

        return data;
    }

    private static string BuildNews(
        string headline, int oldPrice, int price, int oldStock, int stock)
    {
        return headline + "  가격 " + oldPrice + " → " + price
            + "코인 / 구매 한도 " + oldStock + " → " + stock + "개";
    }

    public static bool IsValid(PreparationShopData data)
    {
        if (data == null || data.budget < 0 || data.budget > 100000
            || data.prices == null || data.prices.Length != IngredientCount
            || data.stocks == null || data.stocks.Length != IngredientCount
            || data.cartCounts == null || data.cartCounts.Length != IngredientCount)
        {
            return false;
        }

        long cost = 0;

        for (int i = 0; i < IngredientCount; i++)
        {
            if (data.prices[i] < 1 || data.prices[i] > 999
                || data.stocks[i] < 0 || data.stocks[i] > 99
                || data.cartCounts[i] < 0 || data.cartCounts[i] > data.stocks[i])
            {
                return false;
            }

            cost += (long)data.prices[i] * data.cartCounts[i];
        }

        return cost <= data.budget;
    }

    public static int GetTotalCost(PreparationShopData data)
    {
        int cost = 0;

        for (int i = 0; i < IngredientCount; i++)
        {
            cost += data.prices[i] * data.cartCounts[i];
        }

        return cost;
    }

    public static int GetTotalCount(PreparationShopData data)
    {
        int count = 0;

        for (int i = 0; i < IngredientCount; i++)
        {
            count += data.cartCounts[i];
        }

        return count;
    }

    // 장바구니를 바꿀 때 재고와 예산을 동시에 검사한다.
    public static bool TryChangeQuantity(PreparationShopData data, int index, int change)
    {
        if (!IsValid(data) || index < 0 || index >= IngredientCount
            || (change != 1 && change != -1))
        {
            return false;
        }

        int nextCount = data.cartCounts[index] + change;
        int nextCost = GetTotalCost(data) + data.prices[index] * change;

        if (nextCount < 0 || nextCount > data.stocks[index]
            || nextCost < 0 || nextCost > data.budget)
        {
            return false;
        }

        data.cartCounts[index] = nextCount;
        return true;
    }

    public static PreparationShopData Copy(PreparationShopData data)
    {
        return new PreparationShopData
        {
            budget = data.budget,
            prices = (int[])data.prices.Clone(),
            stocks = (int[])data.stocks.Clone(),
            cartCounts = (int[])data.cartCounts.Clone(),
            harvestNews = data.harvestNews,
            shortageNews = data.shortageNews
        };
    }

    // 6종으로 저장한 상점도 기존 가격, 재고와 장바구니를 유지한다.
    public static PreparationShopData Upgrade(PreparationShopData data)
    {
        if (IsValid(data)) return Copy(data);
        if (data == null || data.budget < 0 || data.budget > 100000
            || data.prices == null || data.prices.Length != 6
            || data.stocks == null || data.stocks.Length != 6
            || data.cartCounts == null || data.cartCounts.Length != 6)
            return null;

        long cost = 0;
        int seed = 17;
        for (int i = 0; i < 6; i++)
        {
            if (data.prices[i] < 1 || data.prices[i] > 999
                || data.stocks[i] < 0 || data.stocks[i] > 99
                || data.cartCounts[i] < 0 || data.cartCounts[i] > data.stocks[i])
                return null;
            cost += (long)data.prices[i] * data.cartCounts[i];
            seed = unchecked(seed * 31 + data.prices[i] * 7 + data.stocks[i]);
        }
        if (cost > data.budget) return null;

        PreparationShopData upgraded = Create(seed);
        Array.Copy(data.prices, upgraded.prices, 6);
        Array.Copy(data.stocks, upgraded.stocks, 6);
        Array.Copy(data.cartCounts, upgraded.cartCounts, 6);
        upgraded.budget = data.budget;
        upgraded.harvestNews = data.harvestNews;
        upgraded.shortageNews = data.shortageNews;
        return upgraded;
    }

    // 구매 확정 시에만 코인과 상점 재고를 소모한다.
    public static PreparationShopData AfterPurchase(PreparationShopData data)
    {
        PreparationShopData result = Copy(data);
        result.budget -= GetTotalCost(data);

        for (int i = 0; i < IngredientCount; i++)
        {
            result.stocks[i] -= result.cartCounts[i];
            result.cartCounts[i] = 0;
        }

        return result;
    }
}
