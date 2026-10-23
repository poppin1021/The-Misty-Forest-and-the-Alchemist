using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// UI 캐릭터의 피격 색상과 흔들림을 처리한다.
[RequireComponent(typeof(Image))]
public class HitReactionUI : MonoBehaviour
{
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private float shakeStrength = 10f;

    private Image targetImage;
    private RectTransform targetRect;
    private Coroutine hitRoutine;

    private Vector2 originalPosition;
    private Color originalColor;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        targetRect = GetComponent<RectTransform>();
    }

    // 연속으로 맞으면 이전 연출을 정리하고 다시 시작한다.
    public void PlayHit()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        StopReaction();

        originalPosition = targetRect.anchoredPosition;
        originalColor = targetImage.color;

        hitRoutine = StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        float elapsed = 0f;
        float reactionDuration = Mathf.Max(0.01f, duration);

        Color hitColor = new Color(
            1f, 0.35f, 0.35f, originalColor.a
        );

        while (elapsed < reactionDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsed / reactionDuration);

            float shake =
                Mathf.Sin(progress * Mathf.PI * 6f)
                * shakeStrength
                * (1f - progress);

            targetRect.anchoredPosition =
                originalPosition + Vector2.right * shake;

            targetImage.color =
                Color.Lerp(hitColor, originalColor, progress);

            yield return null;
        }

        RestoreVisual();
        hitRoutine = null;
    }

    private void RestoreVisual()
    {
        targetRect.anchoredPosition = originalPosition;
        targetImage.color = originalColor;
    }

    private void StopReaction()
    {
        if (hitRoutine == null)
        {
            return;
        }

        StopCoroutine(hitRoutine);
        RestoreVisual();
        hitRoutine = null;
    }

    private void OnDisable()
    {
        StopReaction();
    }
}