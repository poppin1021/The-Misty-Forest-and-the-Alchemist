using System;

public enum PotionTarget
{
    Enemy,
    Self
}

// 물약의 즉시 효과, 첨가제 성질과 다음 턴 효과를 저장한다.
[Serializable]
public class PotionData
{
    public string potionName;
    public string additiveName;
    public PotionTarget target;
    public int damage;
    public int block;
    public int healing;
    public int drawCount;

    public int weakTurns;
    public int vulnerableTurns;
    public int poisonStacks;
    public int burnStacks;

    public int healthCost;
    public bool chooseOneDraw;
    public bool grantsAfterheat;
    public bool repeatNextTurn;
    public int delayedDrawCount;

    // 필드는 수치·문자열뿐이므로 복사본을 안전하게 수정할 수 있다.
    public PotionData Copy()
    {
        return (PotionData)MemberwiseClone();
    }
}
