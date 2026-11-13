using UnityEngine;
using UnityEngine.UI;

// UI 그림의 아래쪽을 기준으로 숨 쉬는 대기 모션을 적용한다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class IdleBreathingUI : BaseMeshEffect
{
    [Header("숨 쉬는 속도")]
    [Min(0.1f)]
    [SerializeField] private float cycleDuration = 2.4f;

    [Header("크기 변화량")]
    [Range(0f, 0.05f)]
    [SerializeField] private float verticalAmount = 0.015f;

    [Range(0f, 0.05f)]
    [SerializeField] private float horizontalAmount = 0.004f;

    [Header("시작 시점 차이")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseOffset;

    private float elapsed;
    private bool isBreathing = true;

    private void Update()
    {
        if (!Application.isPlaying
            || !isBreathing
            || Time.deltaTime <= 0f)
        {
            return;
        }

        float cycle = Mathf.Max(0.1f, cycleDuration);
        elapsed = Mathf.Repeat(elapsed + Time.deltaTime, cycle);
        graphic.SetVerticesDirty();
    }

    // RectTransform을 움직이지 않고 그림의 꼭짓점만 변형한다.
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()
            || !Application.isPlaying
            || !isBreathing
            || vh.currentVertCount == 0)
        {
            return;
        }

        UIVertex vertex = UIVertex.simpleVert;
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            minX = Mathf.Min(minX, vertex.position.x);
            maxX = Mathf.Max(maxX, vertex.position.x);
            minY = Mathf.Min(minY, vertex.position.y);
        }

        Vector2 origin = new Vector2((minX + maxX) * 0.5f, minY);
        float cycle = Mathf.Max(0.1f, cycleDuration);
        float breath = Mathf.Sin(
            (elapsed / cycle + phaseOffset) * Mathf.PI * 2f
        );

        float heightScale = 1f + breath * verticalAmount;
        float widthScale = 1f - breath * horizontalAmount;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            vertex.position.x = origin.x
                + (vertex.position.x - origin.x) * widthScale;
            vertex.position.y = origin.y
                + (vertex.position.y - origin.y) * heightScale;
            vh.SetUIVertex(vertex, i);
        }
    }

    // 전투 종료 시 원래 그림으로 돌아가고 대기 모션을 멈춘다.
    public void StopIdle()
    {
        isBreathing = false;
        graphic.SetVerticesDirty();
    }
}
