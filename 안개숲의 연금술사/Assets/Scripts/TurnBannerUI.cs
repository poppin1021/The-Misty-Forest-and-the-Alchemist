using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상단의 현재 턴 표시와 턴 전환 때 나타나는 중앙 안내를 함께 관리한다.
public class TurnBannerUI : MonoBehaviour
{
    [SerializeField] private float appearDuration = 0.16f;
    [SerializeField] private float holdDuration = 0.45f;
    [SerializeField] private float disappearDuration = 0.16f;

    private TMP_Text currentTurnText;
    private TMP_Text roundText;
    private TMP_Text ownerText;
    private CanvasGroup announcement;
    private RectTransform bannerRect;
    private Image topLine;
    private Image bottomLine;
    private int animationVersion;

    public static TurnBannerUI Create(Transform parent, TMP_FontAsset font, TMP_Text currentTurn)
    {
        GameObject root = new GameObject("TurnPresentation", typeof(RectTransform));
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        Stretch(root.GetComponent<RectTransform>());
        TurnBannerUI ui = root.AddComponent<TurnBannerUI>();
        ui.currentTurnText = currentTurn;
        ui.Build(font);
        return ui;
    }

    private void Build(TMP_FontAsset font)
    {
        GameObject overlay = new GameObject("TurnAnnouncement", typeof(RectTransform), typeof(CanvasGroup));
        overlay.layer = gameObject.layer;
        overlay.transform.SetParent(transform, false);
        Stretch(overlay.GetComponent<RectTransform>());
        announcement = overlay.GetComponent<CanvasGroup>();
        announcement.interactable = false;
        announcement.alpha = 0f;
        announcement.blocksRaycasts = false;

        Image shade = Panel("TurnInputBlocker", overlay.transform, new Color(0f, 0f, 0f, 0.08f));
        Stretch(shade.rectTransform);
        shade.raycastTarget = true;
        Image banner = Panel("TurnBanner", overlay.transform, new Color(0.05f, 0.08f, 0.12f, 0.92f));
        bannerRect = banner.rectTransform;
        bannerRect.anchorMin = new Vector2(0f, 0.5f);
        bannerRect.anchorMax = new Vector2(1f, 0.5f);
        bannerRect.anchoredPosition = new Vector2(0f, 70f);
        bannerRect.sizeDelta = new Vector2(0f, 176f);

        topLine = Panel("TopLine", banner.transform, Color.white);
        SetLine(topLine.rectTransform, 1f);
        bottomLine = Panel("BottomLine", banner.transform, Color.white);
        SetLine(bottomLine.rectTransform, 0f);
        roundText = Label("RoundNumber", banner.transform, font, 27f);
        Center(roundText.rectTransform, 38f, new Vector2(1000f, 42f));
        ownerText = Label("TurnOwner", banner.transform, font, 46f);
        Center(ownerText.rectTransform, -20f, new Vector2(1100f, 74f));
        overlay.SetActive(false);
    }

    public void SetCurrentTurn(int turnNumber, bool enemyTurn)
    {
        string owner = enemyTurn ? "적의 턴" : "플레이어의 턴";
        currentTurnText.text = Mathf.Max(1, turnNumber) + "턴  ·  " + owner;
        currentTurnText.color = TurnColor(enemyTurn);
    }

    // BagController가 기다릴 수 있는 코루틴으로 제공해 실제 행동과 순서를 맞춘다.
    public IEnumerator Play(int turnNumber, bool enemyTurn, bool continuing = false)
    {
        int version = ++animationVersion;
        SetCurrentTurn(turnNumber, enemyTurn);
        roundText.text = Mathf.Max(1, turnNumber) + "턴" + (continuing ? " · 이어하기" : "");
        ownerText.text = enemyTurn ? "적의 턴" : "플레이어의 턴";
        Color accent = TurnColor(enemyTurn);
        ownerText.color = accent;
        topLine.color = bottomLine.color = accent;
        transform.SetAsLastSibling();
        announcement.gameObject.SetActive(true);
        announcement.blocksRaycasts = true;
        float direction = enemyTurn ? 1f : -1f;

        yield return Fade(0f, 1f, direction * 110f, 0f, appearDuration, version);
        if (version != animationVersion) yield break;
        yield return new WaitForSeconds(Mathf.Max(0f, holdDuration));
        if (version != animationVersion) yield break;
        yield return Fade(1f, 0f, 0f, -direction * 80f, disappearDuration, version);
        if (version == animationVersion) HideAnnouncement();
    }

    private IEnumerator Fade(float from, float to, float startX, float endX, float duration, int version)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration && version == animationVersion)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            announcement.alpha = Mathf.Lerp(from, to, progress);
            bannerRect.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, progress), 70f);
            yield return null;
        }
        if (version == animationVersion) announcement.alpha = to;
    }

    public void SetBattleEnded()
    {
        animationVersion++;
        HideAnnouncement();
        currentTurnText.text = "전투 종료";
        currentTurnText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
    }

    private void HideAnnouncement()
    {
        announcement.blocksRaycasts = false;
        announcement.alpha = 0f;
        announcement.gameObject.SetActive(false);
    }

    private static Color TurnColor(bool enemyTurn)
    {
        return enemyTurn ? new Color32(255, 144, 124, 255) : new Color32(143, 214, 255, 255);
    }

    private static Image Panel(string name, Transform parent, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        item.layer = parent.gameObject.layer;
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, float size)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        item.layer = parent.gameObject.layer;
        item.transform.SetParent(parent, false);
        TMP_Text text = item.GetComponent<TMP_Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.92f, 0.94f, 0.97f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Center(RectTransform rect, float y, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = size;
    }

    private static void SetLine(RectTransform rect, float y)
    {
        rect.anchorMin = new Vector2(0f, y);
        rect.anchorMax = new Vector2(1f, y);
        rect.pivot = new Vector2(0.5f, y);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 3f);
    }
}
