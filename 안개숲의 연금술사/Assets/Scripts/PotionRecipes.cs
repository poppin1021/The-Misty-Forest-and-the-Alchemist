using System.Collections.Generic;

// 재료 조합을 판별해 새 물약 데이터를 만든다. 등록되지 않은 조합은 null을 반환한다.
public static class PotionRecipes
{
    public static PotionData Create(List<string> ingredients)
    {
        // 1. 기본 물약 + 졸음가루: 다음 내 턴에 기본 효과를 한 번 더 적용한다.
        if (ingredients.Count == 3
            && ingredients.Contains("졸음나방 날개가루")
            && !ingredients.Contains("시듦버섯"))
        {
            List<string> baseIngredients = new List<string>(ingredients);

            baseIngredients.Remove("졸음나방 날개가루");

            // 첨가제를 뺀 두 재료로 아래의 기본 물약 판별을 재사용한다.
            PotionData potion = Create(baseIngredients);

            if (potion == null)
            {
                return null;
            }

            potion.repeatNextTurn = true;

            return potion;
        }

        // 2. 시듦버섯 + 졸음가루: 적에게 약화를 주는 물약을 만든다.
        if (ingredients.Count == 3
            && ingredients.Contains("시듦버섯")
            && ingredients.Contains("졸음나방 날개가루"))
        {
            List<string> baseIngredients = new List<string>(ingredients);

            baseIngredients.Remove("졸음나방 날개가루");

            if (IsPair(baseIngredients, "홍등열매", "시듦버섯"))
            {
                return new PotionData
                {
                    potionName = "졸음 폭발 물약",
                    target = PotionTarget.Enemy,
                    damage = 6,
                    weakTurns = 1
                };
            }

            if (IsPair(baseIngredients, "갑각잎", "시듦버섯"))
            {
                return new PotionData
                {
                    potionName = "졸음 장막 물약",
                    target = PotionTarget.Enemy,
                    block = 4,
                    weakTurns = 1
                };
            }

            if (IsPair(baseIngredients, "새살꽃", "시듦버섯"))
            {
                return new PotionData
                {
                    potionName = "졸음 치유 물약",
                    target = PotionTarget.Enemy,
                    healing = 3,
                    weakTurns = 1
                };
            }

            if (IsPair(baseIngredients, "바람씨앗", "시듦버섯"))
            {
                return new PotionData
                {
                    potionName = "졸음 바람 물약",
                    target = PotionTarget.Enemy,
                    drawCount = 1,
                    weakTurns = 1
                };
            }

            if (IsPair(baseIngredients, "시듦버섯", "시듦버섯"))
            {
                return new PotionData
                {
                    potionName = "깊은 졸음 물약",
                    target = PotionTarget.Enemy,
                    weakTurns = 2
                };
            }

            return null;
        }

        // 3. 기본 재료 두 개의 조합
        if (ingredients.Count != 2)
        {
            return null;
        }

        if (IsPair(ingredients, "홍등열매", "홍등열매"))
        {
            return new PotionData
            {
                potionName = "폭발 물약",
                target = PotionTarget.Enemy,
                damage = 12
            };
        }

        if (IsPair(ingredients, "홍등열매", "갑각잎"))
        {
            return new PotionData
            {
                potionName = "돌격 물약",
                target = PotionTarget.Enemy,
                damage = 6,
                block = 4
            };
        }

        if (IsPair(ingredients, "홍등열매", "새살꽃"))
        {
            return new PotionData
            {
                potionName = "생기폭발 물약",
                target = PotionTarget.Enemy,
                damage = 6,
                healing = 3
            };
        }

        if (IsPair(ingredients, "홍등열매", "바람씨앗"))
        {
            return new PotionData
            {
                potionName = "질풍 물약",
                target = PotionTarget.Enemy,
                damage = 6,
                drawCount = 1
            };
        }

        if (IsPair(ingredients, "갑각잎", "갑각잎"))
        {
            return new PotionData
            {
                potionName = "철벽 물약",
                target = PotionTarget.Self,
                block = 8
            };
        }

        if (IsPair(ingredients, "갑각잎", "새살꽃"))
        {
            return new PotionData
            {
                potionName = "생명의 장막 물약",
                target = PotionTarget.Self,
                block = 4,
                healing = 3
            };
        }

        if (IsPair(ingredients, "갑각잎", "바람씨앗"))
        {
            return new PotionData
            {
                potionName = "바람 장막 물약",
                target = PotionTarget.Self,
                block = 4,
                drawCount = 1
            };
        }

        if (IsPair(ingredients, "새살꽃", "새살꽃"))
        {
            return new PotionData
            {
                potionName = "치유 물약",
                target = PotionTarget.Self,
                healing = 6
            };
        }

        if (IsPair(ingredients, "새살꽃", "바람씨앗"))
        {
            return new PotionData
            {
                potionName = "생명의 바람 물약",
                target = PotionTarget.Self,
                healing = 3,
                drawCount = 1
            };
        }

        if (IsPair(ingredients, "바람씨앗", "바람씨앗"))
        {
            return new PotionData
            {
                potionName = "태풍 물약",
                target = PotionTarget.Self,
                drawCount = 2
            };
        }

        return null;
    }

    // 재료를 넣은 순서와 관계없이 같은 조합으로 판별한다.
    private static bool IsPair(
        List<string> ingredients,
        string first,
        string second)
    {
        return
            (ingredients[0] == first && ingredients[1] == second)
            || (ingredients[0] == second && ingredients[1] == first);
    }
}
