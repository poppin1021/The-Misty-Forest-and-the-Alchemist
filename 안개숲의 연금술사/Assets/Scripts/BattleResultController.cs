using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleResultController : MonoBehaviour
{
    [Header("전투와 결과 화면")]
    [SerializeField] private BagController bagController;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultMessageText;

    [Header("결과 화면 버튼")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnToTitleButton;

    [Header("결과 표시 대기 시간")]
    [SerializeField] private float resultDelay = 0.35f;

    private bool hasResult;
    private bool isLeaving;

    private void Awake()
    {
        resultPanel.SetActive(false);

        retryButton.onClick.AddListener(RetryBattle);
        returnToTitleButton.onClick.AddListener(ReturnToTitle);
    }

    // 승리 또는 패배에 맞춰 문구를 준비한다.
    public void ShowResult(bool isVictory)
    {
        if (hasResult)
        {
            return;
        }

        hasResult = true;

        resultTitleText.text =
            isVictory ? "전투 승리" : "전투 패배";

        resultMessageText.text = isVictory
            ? "숲의 짐승을 물리쳤습니다."
            : "숲의 짐승에게 쓰러졌습니다.\n재료 조합을 바꿔 다시 도전해보세요.";

        StartCoroutine(OpenResultAfterDelay());
    }

    private IEnumerator OpenResultAfterDelay()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, resultDelay)
        );

        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();
    }

    // 현재 재료 구성을 유지한 재도전 상태를 저장하고 전투를 다시 연다.
    private void RetryBattle()
    {
        if (!CanLeave())
        {
            return;
        }

        GameSaveData retryData =
            bagController.CreateRetryData();

        if (!Application.CanStreamedLevelBeLoaded(retryData.sceneName))
        {
            resultMessageText.text =
                "전투 씬을 불러올 수 없습니다.\nScene List를 확인해주세요.";
            return;
        }

        int slotNumber = GameSession.SlotNumber;

        // 타이틀에서 슬롯을 선택하고 들어온 경우 파일에도 저장한다.
        if (slotNumber >= 1 && slotNumber <= 3)
        {
            if (!SaveSystem.Save(slotNumber, retryData))
            {
                resultMessageText.text =
                    "재도전 정보를 저장하지 못했습니다.\n다시 시도해주세요.";
                return;
            }
        }

        GameSession.SelectSave(slotNumber, retryData);

        BeginLeaving();
        SceneManager.LoadScene(retryData.sceneName);
    }

    private void ReturnToTitle()
    {
        if (!CanLeave())
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded("Title"))
        {
            resultMessageText.text =
                "Title 씬을 불러올 수 없습니다.\nScene List를 확인해주세요.";
            return;
        }

        BeginLeaving();
        SceneManager.LoadScene("Title");
    }

    private bool CanLeave()
    {
        return hasResult
            && resultPanel.activeSelf
            && !isLeaving;
    }

    private void BeginLeaving()
    {
        isLeaving = true;

        retryButton.interactable = false;
        returnToTitleButton.interactable = false;

        Time.timeScale = 1f;
    }
}