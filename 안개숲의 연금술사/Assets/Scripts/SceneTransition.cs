using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 모든 씬이 같은 가림막을 사용하고, 전환 중에는 UI 입력을 잠근다.
public class SceneTransition : MonoBehaviour
{
    [SerializeField] private float fadeOutDuration = 0.22f;
    [SerializeField] private float fadeInDuration = 0.28f;

    private static SceneTransition instance;
    private CanvasGroup cover;
    private EventSystem blockedEventSystem;
    private bool previousNavigation;
    private bool isTransitioning;

    public static bool IsTransitioning => instance != null && instance.isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
        Time.timeScale = 1f;
        instance.isTransitioning = true;
        instance.StartCoroutine(instance.RevealInitialScene());
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        GameObject root = new GameObject("SceneTransition");
        instance = root.AddComponent<SceneTransition>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildCover();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void BuildCover()
    {
        GameObject canvasObject = new GameObject("FadeCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("FadeImage", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = canvasObject.layer;
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        imageObject.GetComponent<Image>().color = Color.black;

        cover = canvasObject.GetComponent<CanvasGroup>();
        cover.alpha = 1f;
        cover.interactable = false;
        cover.blocksRaycasts = true;
    }

    public static bool LoadScene(string sceneName, Action onFailure = null)
    {
        if (IsTransitioning) return false;
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("이동할 씬을 Scene List에서 확인해주세요: " + sceneName);
            return false;
        }

        EnsureInstance();
        instance.isTransitioning = true;
        instance.cover.blocksRaycasts = true;
        instance.BlockNavigation();
        instance.StartCoroutine(instance.SwitchScene(sceneName, onFailure));
        return true;
    }

    private IEnumerator RevealInitialScene()
    {
        // 새 씬의 Awake/Start와 UI 배치가 끝난 뒤 화면을 드러낸다.
        yield return null;
        BlockNavigation();
        yield return FadeTo(0f, fadeInDuration);
        FinishTransition();
    }

    private IEnumerator SwitchScene(string sceneName, Action onFailure)
    {
        float sourceTimeScale = Time.timeScale;
        yield return FadeTo(1f, fadeOutDuration);

        AsyncOperation loading = null;
        try
        {
            Time.timeScale = 1f;
            loading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        if (loading == null)
        {
            Time.timeScale = sourceTimeScale;
            yield return FadeTo(0f, fadeInDuration);
            FinishTransition();
            onFailure?.Invoke();
            yield break;
        }

        yield return loading;
        yield return null;
        Time.timeScale = 1f;
        BlockNavigation();
        yield return FadeTo(0f, fadeInDuration);
        FinishTransition();
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float initial = cover.alpha;
        float elapsed = 0f;
        while (elapsed < Mathf.Max(0.01f, duration))
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            cover.alpha = Mathf.Lerp(initial, target, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }
        cover.alpha = target;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isTransitioning) BlockNavigation();
    }

    private void BlockNavigation()
    {
        EventSystem current = EventSystem.current;
        if (current == null || current == blockedEventSystem) return;
        RestoreNavigation();
        blockedEventSystem = current;
        previousNavigation = current.sendNavigationEvents;
        current.sendNavigationEvents = false;
        current.SetSelectedGameObject(null);
    }

    private void RestoreNavigation()
    {
        if (blockedEventSystem != null)
            blockedEventSystem.sendNavigationEvents = previousNavigation;
        blockedEventSystem = null;
    }

    private void FinishTransition()
    {
        cover.alpha = 0f;
        cover.blocksRaycasts = false;
        RestoreNavigation();
        isTransitioning = false;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RestoreNavigation();
        instance = null;
    }
}
