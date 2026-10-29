using System;

// 슬롯 하나에 저장할 게임 진행 정보다.
[Serializable]
public class GameSaveData
{
    public int saveVersion = 1;
    public string savedAt = "";
    public string sceneName = "Prologue";
    // 스토리를 완료하거나 건너뛰었는지 저장한다.
    public bool hasSeenPrologue = false;

    // 현재 테스트 전투와 같은 초기 수치다.
    public int playerHP = 40;
    public int playerMaxHP = 60;
    public int battleNumber = 1;

    public string[] bagTokens =
{
    "홍등열매", "홍등열매", "홍등열매",
    "갑각잎", "갑각잎",
    "새살꽃", "새살꽃",
    "시듦버섯",
    "바람씨앗",
    "졸음나방 날개가루"
};

    // 전투 중 저장인지 구분한다. 기존 저장 파일은 false로 처리한다.
    public bool hasBattleState = false;

    public int turnNumber = 1;
    public bool hasDrawn = false;

    public int playerArmor = 0;

    public int enemyHP = 30;
    public int enemyMaxHP = 30;
    public int enemyAttackDamage = 8;
    public int enemyWeakTurns = 0;

    // 현재 턴에 남아 있는 재료와 예약된 물약 효과
    public string[] remainingTokens = new string[0];
    public string[] handTokens = new string[0];
    public string[] cauldronTokens = new string[0];
    public PotionData[] delayedPotions = new PotionData[0];

}