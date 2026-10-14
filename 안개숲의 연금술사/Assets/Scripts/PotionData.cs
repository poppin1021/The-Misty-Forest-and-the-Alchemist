using System;

// 물약의 사용 대상
public enum PotionTarget
{
    Enemy,
    Self
}

// 물약 한 개의 이름, 사용 대상과 효과를 보관한다.
[Serializable]
public class PotionData
{
    public string potionName;
    public PotionTarget target;

    // 즉시 적용하는 기본 효과
    public int damage;
    public int block;
    public int healing;
    public int drawCount;

    // 약화는 적의 공격 횟수로 소모하고, 재발동은 다음 내 턴에 한 번 적용한다.
    public int weakTurns;
    public bool repeatNextTurn;
}
