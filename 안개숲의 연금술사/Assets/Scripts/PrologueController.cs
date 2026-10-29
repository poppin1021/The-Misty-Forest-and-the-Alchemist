using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PrologueController : MonoBehaviour
{
    [Header("스토리 화면")]
    [SerializeField] private Image storyImage;
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private TMP_Text nextButtonText;

    [Header("컷별 가림막")]
    [SerializeField] private ComicRevealLayer comicRevealLayer;

    [Header("페이지 순서대로 이미지 연결")]
    [SerializeField] private Sprite[] storyImages = new Sprite[3];

    [Header("연출 속도")]
    [SerializeField] private float characterInterval = 0.04f;
    [SerializeField] private float cutRevealDuration = 0.4f;

    // 페이지별 컷 순서와 해당 컷의 설명이다.
    private readonly string[][] storyCuts =
    {
        new[]
        {
            "숲에서 내려온 짙은 안개가 마을을 뒤덮었다.",

            "그날 이후, 마을 사람들은 하나둘 "
            + "원인 모를 병을 앓기 시작했다."
        },

        new[]
        {
            "안개는 어디에서 생겨났고, "
            + "사람들은 왜 병들기 시작했을까?",

            "견습 연금술사는 그 원인을 직접 확인하기 위해 "
            + "안개숲으로 향했다.",

            "하지만 숲에는 안개의 영향을 받아 "
            + "사나워진 짐승들이 돌아다니고 있었다."
        },

        new[]
        {
            "안개의 근원을 찾으려면 먼저 숲에서 살아남아야 한다.\n"
            + "연금술사는 위험에 대비해 가방을 펼쳤다.",

            "출발하기 전에, "
            + "물약 제조에 사용할 재료 열 개를 챙기자."
        }
    };

    private int pageIndex;
    private int cutIndex;

    private bool isBusy;
    private bool isLeaving;

    private Coroutine animationRoutine;

    private void Start()
    {
        if (comicRevealLayer == null)
        {
            Debug.LogError(
                "PrologueManager의 Comic Reveal Layer를 연결해주세요."
            );
            enabled = false;
            return;
        }

        ShowPage();
    }

    // 페이지를 바꾸면 모든 컷을 가린 뒤 첫 컷부터 시작한다.
    private void ShowPage()
    {
        StopAnimation();

        cutIndex = 0;

        storyImage.overrideSprite = null;
        storyImage.sprite = storyImages[pageIndex];
        storyImage.color = Color.white;
        storyImage.preserveAspect = true;

        comicRevealLayer.PreparePage(pageIndex, storyImage);

        pageText.text =
            (pageIndex + 1) + " / " + storyCuts.Length;

        ShowCurrentCut();
    }

    private void ShowCurrentCut()
    {
        StopAnimation();

        storyText.text = storyCuts[pageIndex][cutIndex];
        storyText.maxVisibleCharacters = 0;
        storyText.ForceMeshUpdate();

        isBusy = true;
        UpdateNextButtonText();

        animationRoutine = StartCoroutine(RevealAndType());
    }

    // 현재 컷의 가림막을 걷어내면서 글자를 순서대로 표시한다.
    private IEnumerator RevealAndType()
    {
        int characterCount = storyText.textInfo.characterCount;

        float interval = Mathf.Max(0.001f, characterInterval);
        float revealDuration = Mathf.Max(0.01f, cutRevealDuration);

        float totalDuration = Mathf.Max(
            revealDuration,
            characterCount * interval
        );

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float revealProgress = Mathf.Clamp01(
                elapsed / revealDuration
            );

            comicRevealLayer.SetReveal(
                cutIndex,
                Mathf.SmoothStep(0f, 1f, revealProgress)
            );

            int visibleCharacters =
                Mathf.FloorToInt(elapsed / interval);

            storyText.maxVisibleCharacters =
                Mathf.Clamp(visibleCharacters, 0, characterCount);

            yield return null;
        }

        comicRevealLayer.SetReveal(cutIndex, 1f);
        storyText.maxVisibleCharacters = int.MaxValue;

        isBusy = false;
        animationRoutine = null;

        UpdateNextButtonText();
    }

    // 연출 중 클릭은 현재 컷 완성, 완료 후 클릭은 다음 컷 이동이다.
    public void NextPage()
    {
        if (isLeaving)
        {
            return;
        }

        if (isBusy)
        {
            CompleteCurrentCut();
            return;
        }

        if (cutIndex < storyCuts[pageIndex].Length - 1)
        {
            cutIndex++;
            ShowCurrentCut();
            return;
        }

        if (pageIndex < storyCuts.Length - 1)
        {
            pageIndex++;
            ShowPage();
            return;
        }

        FinishPrologue();
    }

    private void CompleteCurrentCut()
    {
        StopAnimation();

        comicRevealLayer.SetReveal(cutIndex, 1f);
        storyText.maxVisibleCharacters = int.MaxValue;

        UpdateNextButtonText();
    }

    private void UpdateNextButtonText()
    {
        if (isBusy)
        {
            nextButtonText.text = "바로 보기";
            return;
        }

        bool isLastCut =
            cutIndex == storyCuts[pageIndex].Length - 1;

        bool isLastPage =
            pageIndex == storyCuts.Length - 1;

        if (!isLastCut)
        {
            nextButtonText.text = "다음 컷";
        }
        else if (!isLastPage)
        {
            nextButtonText.text = "다음 페이지";
        }
        else
        {
            nextButtonText.text = "재료 준비하기";
        }
    }

    // 스토리 완료 또는 건너뛰기 후 재료 준비 화면으로 이동한다.
    public void FinishPrologue()
    {
        if (isLeaving)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded("Preparation"))
        {
            Debug.LogError(
                "Scene List에 Preparation 씬을 추가해주세요."
            );
            return;
        }

        int slotNumber = GameSession.SlotNumber;
        GameSaveData currentSave = GameSession.CurrentSave;

        if (slotNumber < 1 || slotNumber > 3 || currentSave == null)
        {
            Debug.LogError(
                "Title 씬에서 빈 슬롯을 선택한 뒤 테스트해주세요."
            );
            return;
        }

        GameSaveData updatedSave =
            JsonUtility.FromJson<GameSaveData>(
                JsonUtility.ToJson(currentSave)
            );

        updatedSave.hasSeenPrologue = true;
        updatedSave.sceneName = "Preparation";
        updatedSave.hasBattleState = false;

        if (!SaveSystem.Save(slotNumber, updatedSave))
        {
            return;
        }

        GameSession.SelectSave(slotNumber, updatedSave);

        isLeaving = true;
        StopAnimation();

        SceneManager.LoadScene("Preparation");
    }

    private void StopAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        isBusy = false;
    }

    private void OnDisable()
    {
        StopAnimation();
    }
}