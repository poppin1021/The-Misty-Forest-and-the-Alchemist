using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("전투와 일시정지 UI")]
    [SerializeField] private BagController bagController;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private TMP_Text pauseStatusText;

    private bool isPaused;
    private bool isLeaving;
    private float previousTimeScale = 1f;

    private void Start()
    {
        pausePanel.SetActive(false);
        pauseStatusText.text = "";
    }

    public void UseSettingsButton(Button button)
    {
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);
        pauseButton = button;
        pauseButton.onClick.AddListener(OpenPauseMenu);
    }

    private void Update()
    {
        pauseButton.interactable =
            !isPaused && !isLeaving && bagController.CanPauseBattle();
    }

    public void OpenPauseMenu()
    {
        if (isPaused || isLeaving || !bagController.PauseBattle())
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        isPaused = true;
        Time.timeScale = 0f;

        pauseStatusText.text = "";
        pausePanel.SetActive(true);
        pausePanel.transform.SetAsLastSibling();
    }

    public void ResumeGame()
    {
        if (!isPaused || isLeaving)
        {
            return;
        }

        pausePanel.SetActive(false);

        Time.timeScale = previousTimeScale;
        isPaused = false;

        bagController.ResumeBattle();
    }

    // 저장 후에도 일시정지 메뉴를 유지한다.
    public void SaveGame()
    {
        if (!isPaused || isLeaving)
        {
            return;
        }

        bagController.SaveBattle(out string message);
        pauseStatusText.text = message;
    }

    // 저장에 성공했을 때만 타이틀로 이동한다.
    public void SaveAndReturnToTitle()
    {
        if (!isPaused || isLeaving)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded("Title"))
        {
            pauseStatusText.text = "Title 씬을 불러올 수 없습니다.";
            return;
        }

        if (!bagController.SaveBattle(out string message))
        {
            pauseStatusText.text = message;
            return;
        }

        isLeaving = true;
        if (!SceneTransition.LoadScene("Title", CancelLeaving))
            CancelLeaving();
    }

    private void OnDestroy()
    {
        if (isPaused)
        {
            Time.timeScale = previousTimeScale;
        }
    }

    private void CancelLeaving()
    {
        isLeaving = false;
        pauseStatusText.text = "타이틀로 이동하지 못했습니다. 다시 시도해주세요.";
    }

}