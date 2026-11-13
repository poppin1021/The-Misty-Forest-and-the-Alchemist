using System;

// 조합, 상점과 안내창에서 같은 재료 이름과 분류를 사용한다.
public static class IngredientCatalog
{
    public const int Count = 9;
    public const string Mushroom = "시듦버섯";
    public const string Powder = "졸음나방 날개가루";
    public const string Slime = "허물달팽이 진액";
    public const string Thorn = "썩은별 가시";
    public const string Scale = "불씨도롱뇽 비늘";

    public static readonly string[] BaseIngredients =
    {
        "홍등열매", "갑각잎", "새살꽃", Mushroom, "바람씨앗"
    };

    public static readonly string[] Additives = { Powder, Slime, Thorn, Scale };

    // 앞의 여섯 칸은 기존 상점 저장 파일과 같은 순서다.
    public static readonly string[] AllIngredients =
    {
        "홍등열매", "갑각잎", "새살꽃", Mushroom, "바람씨앗",
        Powder, Slime, Thorn, Scale
    };

    public static string Normalize(string name)
    {
        if (name == null) return null;
        switch (name.Trim())
        {
            case "졸음가루":
            case "수면가루": return Powder;
            case "달팽이 진액": return Slime;
            case "불씨 비늘": return Scale;
            default: return name.Trim();
        }
    }

    public static bool IsBase(string name)
    {
        return Array.IndexOf(BaseIngredients, Normalize(name)) >= 0;
    }

    public static bool IsAdditive(string name)
    {
        return Array.IndexOf(Additives, Normalize(name)) >= 0;
    }

    public static string DisplayName(string name)
    {
        name = Normalize(name);
        return name == Powder ? "졸음가루" : name;
    }

    public static string AdditiveDescription(string name)
    {
        switch (Normalize(name))
        {
            case Powder:
                return "버섯 없음: 다음 내 턴에 피해·방어·회복 재발동 (바람씨앗은 1개 추출)\n버섯 포함: 적에게 약화";
            case Slime:
                return "버섯 없음: 피해·방어·회복 50% 증가, 바람씨앗 추출량 +1\n버섯 포함: 적에게 취약";
            case Thorn:
                return "버섯 없음: 체력 4 소모, 피해·방어·회복 2배, 추출 중 1개 선택\n버섯 포함: 적에게 중독";
            case Scale:
                return "버섯 없음: 다음 물약 하나를 강화하는 잔열 획득\n버섯 포함: 적에게 화상";
            default:
                return "";
        }
    }
}
