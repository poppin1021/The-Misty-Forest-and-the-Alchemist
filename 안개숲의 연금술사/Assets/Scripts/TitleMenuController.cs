using UnityEngine;

// 타이틀 메뉴와 슬롯 선택 화면을 전환한다.
public class TitleMenuController : MonoBehaviour
{
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject saveSlotPanel;

    private void Start()
    {
        GameSession.Clear();
        ShowTitle();
    }

    public void ShowTitle()
    {
        saveSlotPanel.SetActive(false);
        titlePanel.SetActive(true);
    }

    public void OpenSaveSlots()
    {
        titlePanel.SetActive(false);
        saveSlotPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료 요청");
        Application.Quit();
    }
}