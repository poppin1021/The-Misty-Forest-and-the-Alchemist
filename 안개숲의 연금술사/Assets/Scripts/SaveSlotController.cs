using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 슬롯 목록 표시와 새 게임·이어하기를 처리한다.
public class SaveSlotController : MonoBehaviour
{
    [SerializeField] private Button[] slotButtons = new Button[3];

    private bool isLoading;

    private void Awake()
    {
        for (int i = 0; i < slotButtons.Length && i < 3; i++)
        {
            if (slotButtons[i] == null)
            {
                continue;
            }

            int slotNumber = i + 1;

            slotButtons[i].onClick.AddListener(
                () => SelectSlot(slotNumber)
            );
        }
    }

    // 슬롯 창을 열 때마다 저장 파일을 확인한다.
    private void OnEnable()
    {
        for (int i = 0; i < slotButtons.Length && i < 3; i++)
        {
            Button button = slotButtons[i];

            if (button == null)
            {
                continue;
            }

            int slotNumber = i + 1;
            bool hasSave = SaveSystem.HasSave(slotNumber);

            GameSaveData data = hasSave
                ? SaveSystem.Load(slotNumber)
                : null;

            button.interactable = !hasSave || data != null;

            TMP_Text buttonText =
                button.GetComponentInChildren<TMP_Text>(true);

            if (buttonText == null)
            {
                continue;
            }

            if (!hasSave)
            {
                buttonText.text =
                    "슬롯 " + slotNumber + "\n새로 시작하기";
            }
            else if (data != null)
            {
                buttonText.text =
                    "슬롯 " + slotNumber
                    + "\n이어하기"
                    + "\nHP " + data.playerHP
                    + " / " + data.playerMaxHP;
            }
            else
            {
                buttonText.text =
                    "슬롯 " + slotNumber + "\n불러오기 실패";
            }
        }
    }

    private void SelectSlot(int slotNumber)
    {
        if (isLoading)
        {
            return;
        }

        bool hasSave = SaveSystem.HasSave(slotNumber);

        GameSaveData data = hasSave
            ? SaveSystem.Load(slotNumber)
            : new GameSaveData();

        if (data == null)
        {
            Debug.LogError("저장 데이터를 불러오지 못했습니다.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(data.sceneName))
        {
            Debug.LogError(
                "Scene List에 씬을 추가해주세요: " + data.sceneName
            );
            return;
        }

        // 기존 저장은 유지하고, 빈 슬롯에만 새 파일을 만든다.
        if (!hasSave && !SaveSystem.Save(slotNumber, data))
        {
            return;
        }

        GameSession.SelectSave(slotNumber, data);

        isLoading = true;
        SceneManager.LoadScene(data.sceneName);
    }
}