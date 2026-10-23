using System;
using System.IO;
using UnityEngine;

// 세 개의 슬롯을 각각 별도 JSON 파일로 저장하고 불러온다.
public static class SaveSystem
{
    private static string GetSavePath(int slotNumber)
    {
        if (slotNumber < 1 || slotNumber > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotNumber),
                "슬롯 번호는 1~3이어야 합니다."
            );
        }

        return Path.Combine(
            Application.persistentDataPath,
            "save_slot_" + slotNumber + ".json"
        );
    }

    public static bool HasSave(int slotNumber)
    {
        return File.Exists(GetSavePath(slotNumber));
    }

    // 저장에 성공했을 때만 true를 반환한다.
    public static bool Save(int slotNumber, GameSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("저장할 데이터가 없습니다.");
            return false;
        }

        try
        {
            Directory.CreateDirectory(
                Application.persistentDataPath
            );

            data.savedAt =
                DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            string json = JsonUtility.ToJson(data, true);
            string path = GetSavePath(slotNumber);

            File.WriteAllText(path, json);

            Debug.Log("저장 완료: " + path);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("저장 실패: " + exception.Message);
            return false;
        }
    }

    // 파일이 없거나 읽기에 실패하면 null을 반환한다.
    public static GameSaveData Load(int slotNumber)
    {
        string path = GetSavePath(slotNumber);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);

            GameSaveData data =
                JsonUtility.FromJson<GameSaveData>(json);

            if (data == null || data.saveVersion != 1)
            {
                throw new InvalidDataException(
                    "지원하지 않는 저장 데이터입니다."
                );
            }

            return data;
        }
        catch (Exception exception)
        {
            Debug.LogError("불러오기 실패: " + exception.Message);
            return null;
        }
    }
}