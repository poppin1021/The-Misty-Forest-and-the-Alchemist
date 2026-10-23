// 현재 선택한 저장 슬롯과 데이터를 보관한다.
public static class GameSession
{
    public static int SlotNumber { get; private set; }
    public static GameSaveData CurrentSave { get; private set; }

    public static void SelectSave(int slotNumber, GameSaveData data)
    {
        SlotNumber = slotNumber;
        CurrentSave = data;
    }

    public static void Clear()
    {
        SlotNumber = 0;
        CurrentSave = null;
    }
}