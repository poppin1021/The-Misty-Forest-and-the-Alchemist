using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveSlotController : MonoBehaviour
{
    [Header("슬롯 순서대로 연결")]
    [SerializeField] private Button[] slotButtons = new Button[3];
    [SerializeField] private Button[] deleteButtons = new Button[3];

    [Header("삭제 확인 창")]
    [SerializeField] private GameObject deleteConfirmPanel;
    [SerializeField] private TMP_Text deleteMessageText;
    [SerializeField] private Button confirmDeleteButton;
    [SerializeField] private Button cancelDeleteButton;

    private bool isLoading;
    private int pendingDeleteSlot;

    // 버튼 이벤트는 여기서 한 번만 연결한다.
    private void Awake()
    {
        for (int i = 0; i < 3; i++)
        {
            int slotNumber = i + 1;

            if (i < slotButtons.Length && slotButtons[i] != null)
            {
                slotButtons[i].onClick.AddListener(
                    () => SelectSlot(slotNumber)
                );
            }

            if (i < deleteButtons.Length && deleteButtons[i] != null)
            {
                deleteButtons[i].onClick.AddListener(
                    () => RequestDelete(slotNumber)
                );
            }
        }

        confirmDeleteButton.onClick.AddListener(ConfirmDelete);
        cancelDeleteButton.onClick.AddListener(CancelDelete);
    }

    private void OnEnable()
    {
        CancelDelete();
    }

    private void OnDisable()
    {
        pendingDeleteSlot = 0;

        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false);
        }
    }

    // 저장 상태에 따라 슬롯 문구와 버튼 상태를 갱신한다.
    private void RefreshSlots()
    {
        bool isConfirming = pendingDeleteSlot != 0;

        for (int i = 0; i < 3; i++)
        {
            int slotNumber = i + 1;
            bool hasSave = SaveSystem.HasSave(slotNumber);

            GameSaveData data = hasSave
                ? SaveSystem.Load(slotNumber)
                : null;

            if (i < deleteButtons.Length && deleteButtons[i] != null)
            {
                deleteButtons[i].interactable =
                    !isLoading && !isConfirming && hasSave;
            }

            if (i >= slotButtons.Length || slotButtons[i] == null)
            {
                continue;
            }

            Button button = slotButtons[i];

            button.interactable =
                !isLoading
                && !isConfirming
                && (!hasSave || data != null);

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

    // 삭제할 슬롯을 기억하고 확인 창을 연다.
    private void RequestDelete(int slotNumber)
    {
        if (isLoading
            || pendingDeleteSlot != 0
            || !SaveSystem.HasSave(slotNumber))
        {
            return;
        }

        pendingDeleteSlot = slotNumber;

        deleteMessageText.text =
            "슬롯 " + slotNumber + "의 저장 데이터를 삭제할까요?"
            + "\n삭제한 데이터는 복구할 수 없습니다.";

        confirmDeleteButton.interactable = true;

        deleteConfirmPanel.SetActive(true);
        deleteConfirmPanel.transform.SetAsLastSibling();

        RefreshSlots();
    }

    // 확인 버튼을 눌렀을 때만 실제 파일을 삭제한다.
    private void ConfirmDelete()
    {
        if (isLoading
            || pendingDeleteSlot < 1
            || pendingDeleteSlot > 3)
        {
            return;
        }

        int slotNumber = pendingDeleteSlot;

        if (!SaveSystem.Delete(slotNumber))
        {
            deleteMessageText.text =
                "삭제하지 못했습니다.\n다시 시도하거나 취소해주세요.";
            return;
        }

        if (GameSession.SlotNumber == slotNumber)
        {
            GameSession.Clear();
        }

        CancelDelete();
    }

    private void CancelDelete()
    {
        pendingDeleteSlot = 0;
        deleteConfirmPanel.SetActive(false);

        RefreshSlots();
    }

    // 빈 슬롯은 새 게임, 저장된 슬롯은 이어하기로 진입한다.
    private void SelectSlot(int slotNumber)
    {
        if (isLoading || pendingDeleteSlot != 0)
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

        if (!hasSave && !SaveSystem.Save(slotNumber, data))
        {
            return;
        }

        GameSession.SelectSave(slotNumber, data);

        isLoading = true;
        SceneManager.LoadScene(data.sceneName);
    }
}