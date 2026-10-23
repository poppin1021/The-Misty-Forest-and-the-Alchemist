using System.Collections;
using UnityEngine;

// UI 이미지의 돌진과 원위치 복귀를 처리한다.
[RequireComponent(typeof(RectTransform))]
public class AttackMotionUI : MonoBehaviour
{
    [Header("돌진 거리와 시간")]
    [SerializeField] private float lungeDistance = 100f;
    [SerializeField] private float forwardDuration = 0.12f;
    [SerializeField] private float impactHoldDuration = 0.06f;
    [SerializeField] private float returnDuration = 0.18f;

    private RectTransform targetRect;

    private void Awake()
    {
        targetRect = GetComponent<RectTransform>();
    }

    // 가장 앞으로 이동한 순간에 전달받은 공격 함수를 실행한다.
    public IEnumerator PlayAttack(System.Action onImpact)
    {
        if (!isActiveAndEnabled)
        {
            onImpact?.Invoke();
            yield break;
        }

        Vector2 startPosition = targetRect.anchoredPosition;
        Vector2 attackPosition =
            startPosition + Vector2.left * lungeDistance;

        try
        {
            yield return MoveTo(
                startPosition, attackPosition, forwardDuration);

            onImpact?.Invoke();

            yield return new WaitForSeconds(
                Mathf.Max(0f, impactHoldDuration));

            yield return MoveTo(
                attackPosition, startPosition, returnDuration);
        }
        finally
        {
            // 연출이 끝나거나 중단되면 원래 위치로 돌려놓는다.
            if (targetRect != null)
            {
                targetRect.anchoredPosition = startPosition;
            }
        }
    }

    private IEnumerator MoveTo(
        Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        float moveDuration = Mathf.Max(0.01f, duration);

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsed / moveDuration);

            float easedProgress =
                Mathf.SmoothStep(0f, 1f, progress);

            targetRect.anchoredPosition =
                Vector2.Lerp(from, to, easedProgress);

            yield return null;
        }

        targetRect.anchoredPosition = to;
    }
}