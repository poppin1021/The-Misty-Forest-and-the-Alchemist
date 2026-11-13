using System;
using System.Collections.Generic;

// 기본 재료 15조합과 첨가제 4종을 공통 규칙으로 결합한다.
public static class PotionRecipes
{
    public static PotionData Create(List<string> ingredients)
    {
        if (ingredients == null || ingredients.Count < 2 || ingredients.Count > 3)
            return null;

        List<string> bases = new List<string>();
        string additive = null;

        foreach (string item in ingredients)
        {
            string ingredient = IngredientCatalog.Normalize(item);
            if (IngredientCatalog.IsBase(ingredient))
                bases.Add(ingredient);
            else if (IngredientCatalog.IsAdditive(ingredient) && additive == null)
                additive = ingredient;
            else
                return null;
        }

        if (bases.Count != 2) return null;
        int mushrooms = bases.FindAll(item => item == IngredientCatalog.Mushroom).Count;
        if (mushrooms > 0 && additive == null) return null;

        PotionData potion = new PotionData();
        foreach (string ingredient in bases)
        {
            switch (ingredient)
            {
                case "홍등열매": potion.damage += 6; break;
                case "갑각잎": potion.block += 4; break;
                case "새살꽃": potion.healing += 3; break;
                case "바람씨앗": potion.drawCount++; break;
            }
        }

        string baseName = GetBaseName(bases);
        potion.additiveName = additive;

        if (mushrooms > 0)
        {
            int strength = mushrooms == 2 ? 2 : 1;
            switch (additive)
            {
                case IngredientCatalog.Powder:
                    potion.weakTurns = strength;
                    potion.potionName = GetDebuffName(bases, "졸음", "깊은 졸음 물약");
                    break;
                case IngredientCatalog.Slime:
                    potion.vulnerableTurns = strength;
                    potion.potionName = GetDebuffName(bases, "취약", "강한 취약 물약");
                    break;
                case IngredientCatalog.Thorn:
                    potion.poisonStacks = strength * 2;
                    potion.potionName = GetDebuffName(bases, "중독", "짙은 중독 물약");
                    break;
                case IngredientCatalog.Scale:
                    potion.burnStacks = strength * 2;
                    potion.potionName = GetDebuffName(bases, "화상", "거센 화상 물약");
                    break;
            }
        }
        else
        {
            potion.potionName = baseName;
            switch (additive)
            {
                case IngredientCatalog.Powder:
                    potion.potionName = "잔향 " + baseName;
                    potion.repeatNextTurn = true;
                    potion.delayedDrawCount = potion.drawCount > 0 ? 1 : 0;
                    break;
                case IngredientCatalog.Slime:
                    potion.potionName = "농축 " + baseName;
                    potion.damage = PotionEffects.ScaleUp(potion.damage, 3, 2);
                    potion.block = PotionEffects.ScaleUp(potion.block, 3, 2);
                    potion.healing = PotionEffects.ScaleUp(potion.healing, 3, 2);
                    if (potion.drawCount > 0) potion.drawCount++;
                    break;
                case IngredientCatalog.Thorn:
                    potion.potionName = "위험한 " + baseName;
                    potion.healthCost = 4;
                    potion.damage *= 2;
                    potion.block *= 2;
                    potion.healing *= 2;
                    potion.chooseOneDraw = potion.drawCount > 0;
                    break;
                case IngredientCatalog.Scale:
                    potion.potionName = "잔열 " + baseName;
                    potion.grantsAfterheat = true;
                    break;
            }
        }

        potion.target = potion.damage > 0 || mushrooms > 0
            ? PotionTarget.Enemy : PotionTarget.Self;
        return potion;
    }

    public static string GetBaseName(IList<string> ingredients)
    {
        if (ingredients == null || ingredients.Count != 2) return "";
        int a = Array.IndexOf(IngredientCatalog.BaseIngredients, IngredientCatalog.Normalize(ingredients[0]));
        int b = Array.IndexOf(IngredientCatalog.BaseIngredients, IngredientCatalog.Normalize(ingredients[1]));
        if (a < 0 || b < 0) return "";
        if (a > b) { int temporary = a; a = b; b = temporary; }

        string[,] names =
        {
            { "폭발 물약", "돌격 물약", "생기폭발 물약", "포자 폭발 물약", "질풍 물약" },
            { "", "철벽 물약", "생명의 장막 물약", "포자 장막 물약", "바람 장막 물약" },
            { "", "", "치유 물약", "포자 치유 물약", "생명의 바람 물약" },
            { "", "", "", "짙은 포자 물약", "포자 바람 물약" },
            { "", "", "", "", "태풍 물약" }
        };
        return names[a, b];
    }

    private static string GetDebuffName(List<string> bases, string prefix, string doubleName)
    {
        if (bases[0] == IngredientCatalog.Mushroom && bases[1] == IngredientCatalog.Mushroom)
            return doubleName;
        string other = bases[0] == IngredientCatalog.Mushroom ? bases[1] : bases[0];
        switch (other)
        {
            case "홍등열매": return prefix + " 폭발 물약";
            case "갑각잎": return prefix + " 장막 물약";
            case "새살꽃": return prefix + " 치유 물약";
            default: return prefix + " 바람 물약";
        }
    }
}
