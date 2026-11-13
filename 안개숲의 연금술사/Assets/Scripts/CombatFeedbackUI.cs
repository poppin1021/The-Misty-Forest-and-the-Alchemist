using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CombatFeedbackKind { Damage, Healing, Armor, Poison, Burn, Debuff, Heat, Draw, Repeat, HealthCost }

// 같은 대상에 여러 효과가 생겨도 숫자를 시간과 높이로 나누어 표시한다.
public class CombatFeedbackUI : MonoBehaviour
{
    [SerializeField] private float textDuration = 1.05f;
    [SerializeField] private float riseDistance = 28f;
    [SerializeField] private float messageInterval = 0.06f;

    [Header("플레이어 피격 가장자리 효과")]
    [SerializeField] private float damageFlashDuration = 0.4f;
    [SerializeField, Range(0f, 1f)] private float damageFlashOpacity = 0.52f;

    private DamageVignetteGraphic damageVignette;
    private Coroutine damageFlashRoutine;
    private RectTransform canvasRect;
    private TMP_FontAsset font;
    private CanvasGroup visibility;
    private readonly Stack<TMP_Text> textPool = new Stack<TMP_Text>();
    private readonly Dictionary<RectTransform, bool[]> lanes = new Dictionary<RectTransform, bool[]>();

    public static CombatFeedbackUI Create(Transform parent, TMP_FontAsset fontAsset)
    {
        GameObject root = new GameObject("CombatFeedback", typeof(RectTransform), typeof(CanvasGroup));
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        CombatFeedbackUI feedback = root.AddComponent<CombatFeedbackUI>();
        feedback.canvasRect = rect;
        feedback.font = fontAsset;
        feedback.visibility = root.GetComponent<CanvasGroup>();
        feedback.visibility.blocksRaycasts = false;
        feedback.visibility.interactable = false;
        feedback.CreateDamageVignette();
        return feedback;
    }

    public void Show(RectTransform target, string message, CombatFeedbackKind kind)
    {
        if (target == null || string.IsNullOrEmpty(message)) return;
        if (!lanes.ContainsKey(target)) lanes[target] = new bool[4];
        Vector2 position = canvasRect.InverseTransformPoint(target.TransformPoint(target.rect.center));
        position.x = Mathf.Clamp(position.x, canvasRect.rect.xMin + 210f, canvasRect.rect.xMax - 210f);
        position.y = Mathf.Clamp(position.y, canvasRect.rect.yMin + 60f, canvasRect.rect.yMax - 300f);
        StartCoroutine(FloatText(message, kind, position, lanes[target]));
    }

    public void Pulse(RectTransform target, CombatFeedbackKind kind)
    {
        if (target == null) return;
        Vector2 position = canvasRect.InverseTransformPoint(target.TransformPoint(target.rect.center));
        StartCoroutine(Impact(position, Tint(kind)));
    }

    public void SetVisible(bool visible)
    {
        visibility.alpha = visible ? 1f : 0f;
    }

    private TMP_Text AcquireText()
    {
        TMP_Text text;
        if (textPool.Count > 0) text = textPool.Pop();
        else
        {
            GameObject label = new GameObject("CombatNumber", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.layer = gameObject.layer;
            label.transform.SetParent(transform, false);
            text = label.GetComponent<TMP_Text>();
            text.font = font;
            text.fontSize = 34f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.richText = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.outlineColor = new Color32(20, 23, 28, 255);
            text.outlineWidth = 0.22f;
            text.rectTransform.sizeDelta = new Vector2(460f, 66f);
        }
        text.gameObject.SetActive(true);
        return text;
    }

    private IEnumerator FloatText(string message, CombatFeedbackKind kind, Vector2 position, bool[] occupied)
    {
        int lane = System.Array.IndexOf(occupied, false);
        while (lane < 0)
        {
            yield return null;
            lane = System.Array.IndexOf(occupied, false);
        }
        occupied[lane] = true;
        position.y += 34f + lane * 64f;
        if (lane > 0) yield return new WaitForSeconds(lane * messageInterval);
        TMP_Text label = AcquireText();
        label.text = message;
        label.color = Tint(kind);
        float elapsed = 0f;
        while (elapsed < textDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / textDuration);
            label.rectTransform.anchoredPosition = position + Vector2.up * riseDistance * progress;
            float appear = Mathf.Clamp01(progress / 0.12f);
            float pop = Mathf.Lerp(1.28f, 1f, appear);
            label.rectTransform.localScale = Vector3.one * pop;
            label.alpha = progress < 0.6f ? 1f : 1f - Mathf.InverseLerp(0.6f, 1f, progress);
            yield return null;
        }
        label.gameObject.SetActive(false);
        textPool.Push(label);
        occupied[lane] = false;
    }

    private IEnumerator Impact(Vector2 position, Color tint)
    {
        GameObject pulse = new GameObject("ImpactPulse", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(CombatImpactGraphic));
        pulse.layer = gameObject.layer;
        pulse.transform.SetParent(transform, false);
        pulse.transform.SetAsFirstSibling();
        RectTransform rect = pulse.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        CombatImpactGraphic image = pulse.GetComponent<CombatImpactGraphic>();
        image.raycastTarget = false;
        float elapsed = 0f;
        const float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            rect.sizeDelta = Vector2.one * Mathf.Lerp(42f, 150f, progress);
            tint.a = (1f - progress) * 0.85f;
            image.color = tint;
            yield return null;
        }
        Destroy(pulse);
    }

    private static Color Tint(CombatFeedbackKind kind)
    {
        switch (kind)
        {
            case CombatFeedbackKind.Damage: return new Color32(255, 120, 100, 255);
            case CombatFeedbackKind.Healing: return new Color32(128, 244, 165, 255);
            case CombatFeedbackKind.Armor: return new Color32(150, 210, 255, 255);
            case CombatFeedbackKind.Poison: return new Color32(185, 235, 115, 255);
            case CombatFeedbackKind.Burn: return new Color32(255, 177, 95, 255);
            case CombatFeedbackKind.Debuff: return new Color32(220, 175, 255, 255);
            case CombatFeedbackKind.Heat: return new Color32(255, 215, 115, 255);
            case CombatFeedbackKind.Draw: return new Color32(160, 235, 225, 255);
            case CombatFeedbackKind.Repeat: return new Color32(215, 190, 255, 255);
            default: return new Color32(245, 165, 165, 255);
        }
    }

    private void CreateDamageVignette()
    {
        GameObject overlay = new GameObject("PlayerDamageVignette", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(DamageVignetteGraphic));
        overlay.layer = gameObject.layer;
        overlay.transform.SetParent(transform, false);
        overlay.transform.SetAsFirstSibling();
        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        damageVignette = overlay.GetComponent<DamageVignetteGraphic>();
        damageVignette.raycastTarget = false;
        damageVignette.color = new Color(0.92f, 0.10f, 0.14f, 0f);
        damageVignette.enabled = false;
    }

    // 연속 피격은 현재 밝기에서 다시 시작해 깜빡임이 겹쳐 과해지지 않게 한다.
    public void FlashPlayerDamage()
    {
        if (!isActiveAndEnabled || damageVignette == null) return;
        if (damageFlashRoutine != null) StopCoroutine(damageFlashRoutine);
        damageVignette.enabled = true;
        damageFlashRoutine = StartCoroutine(DamageFlash());
    }

    private IEnumerator DamageFlash()
    {
        float duration = Mathf.Max(0.05f, damageFlashDuration);
        float riseDuration = Mathf.Min(0.06f, duration * 0.25f);
        float initialAlpha = damageVignette.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha;
            if (elapsed < riseDuration)
                alpha = Mathf.Lerp(initialAlpha, damageFlashOpacity,
                    Mathf.SmoothStep(0f, 1f, elapsed / riseDuration));
            else
                alpha = Mathf.Lerp(damageFlashOpacity, 0f,
                    Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(riseDuration, duration, elapsed)));
            Color tint = damageVignette.color;
            tint.a = alpha;
            damageVignette.color = tint;
            yield return null;
        }
        Color clear = damageVignette.color;
        clear.a = 0f;
        damageVignette.color = clear;
        damageVignette.enabled = false;
        damageFlashRoutine = null;
    }
}
