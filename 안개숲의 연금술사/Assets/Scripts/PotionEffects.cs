using System;
using System.Collections.Generic;

public struct PotionDamage
{
    public int direct;
    public int burn;
    public int Total => direct + burn;
}

// UI 미리보기, 실제 적용과 테스트가 같은 계산 규칙을 사용한다.
public static class PotionEffects
{
    public static int ScaleUp(int value, int numerator, int denominator)
    {
        return value <= 0 ? 0 : (value * numerator + denominator - 1) / denominator;
    }

    public static PotionData WithAfterheat(PotionData source, bool hasAfterheat)
    {
        if (source == null) return null;
        PotionData potion = source.Copy();
        if (!hasAfterheat) return potion;

        potion.damage = ScaleUp(potion.damage, 5, 4);
        potion.block = ScaleUp(potion.block, 5, 4);
        potion.healing = ScaleUp(potion.healing, 5, 4);
        if (potion.drawCount > 0) potion.drawCount++;
        if (potion.weakTurns > 0) potion.weakTurns++;
        if (potion.vulnerableTurns > 0) potion.vulnerableTurns++;
        if (potion.poisonStacks > 0) potion.poisonStacks++;
        if (potion.burnStacks > 0) potion.burnStacks++;
        return potion;
    }

    public static PotionData CreateRepeat(PotionData source)
    {
        PotionData repeated = source.Copy();
        repeated.drawCount = source.delayedDrawCount;
        repeated.delayedDrawCount = 0;
        repeated.repeatNextTurn = false;
        repeated.healthCost = 0;
        repeated.chooseOneDraw = false;
        repeated.grantsAfterheat = false;
        return repeated;
    }

    public static PotionDamage CalculateDirectDamage(int baseDamage, int vulnerableTurns, int burnStacks)
    {
        if (baseDamage <= 0) return new PotionDamage();
        return new PotionDamage
        {
            direct = vulnerableTurns > 0 ? ScaleUp(baseDamage, 3, 2) : baseDamage,
            burn = Math.Max(0, burnStacks)
        };
    }

    public static int CalculatePoisonDamage(int poisonStacks)
    {
        return Math.Max(0, poisonStacks);
    }

    public static string Describe(PotionData potion)
    {
        if (potion == null) return "제조할 수 없는 조합입니다.";
        List<string> lines = new List<string> { potion.potionName };
        if (potion.damage > 0) lines.Add("적에게 기본 피해 " + potion.damage);
        if (potion.block > 0) lines.Add("자신에게 방어도 " + potion.block);
        if (potion.healing > 0) lines.Add("자신의 체력 회복 " + potion.healing);
        if (potion.drawCount > 0)
            lines.Add("재료 " + potion.drawCount + "개 추출"
                + (potion.chooseOneDraw ? " (그중 1개 직접 선택)" : ""));
        if (potion.weakTurns > 0) lines.Add("약화 " + potion.weakTurns + "회: 적 공격력 25% 감소");
        if (potion.vulnerableTurns > 0) lines.Add("취약 " + potion.vulnerableTurns + "턴: 직접 피해 50% 증가");
        if (potion.poisonStacks > 0) lines.Add("중독 " + potion.poisonStacks + ": 적 행동 전 피해, 이후 1 감소");
        if (potion.burnStacks > 0) lines.Add("화상 " + potion.burnStacks + ": 직접 공격에 추가 피해, 적 턴 종료 시 1 감소");
        if (potion.healthCost > 0) lines.Add("체력 " + potion.healthCost + " 소모 (방어도 무시)");
        if (potion.repeatNextTurn)
        {
            List<string> repeated = new List<string>();
            if (potion.damage > 0) repeated.Add("피해 " + potion.damage);
            if (potion.block > 0) repeated.Add("방어도 " + potion.block);
            if (potion.healing > 0) repeated.Add("회복 " + potion.healing);
            if (potion.delayedDrawCount > 0) repeated.Add("재료 1개 추출");
            if (repeated.Count > 0) lines.Add("다음 내 턴: " + string.Join(", ", repeated));
        }
        if (potion.grantsAfterheat) lines.Add("잔열: 이번 턴 다음 물약 1개 강화");
        lines.Add("대상: " + (potion.target == PotionTarget.Enemy ? "적" : "자신"));
        return string.Join("\n", lines);
    }
}
