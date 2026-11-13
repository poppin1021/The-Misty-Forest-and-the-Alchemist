using UnityEngine;
using UnityEngine.UI;

// 화면 중앙은 투명하게 두고, 네 가장자리의 붉은색을 안쪽으로 부드럽게 흐린다.
[RequireComponent(typeof(CanvasRenderer))]
public class DamageVignetteGraphic : MaskableGraphic
{
    [SerializeField, Range(0.04f, 0.35f)] private float edgeWidth = 0.18f;
    private static readonly float[] FalloffSteps = { 0f, 0.15f, 0.4f, 0.7f, 1f };

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        Rect bounds = rectTransform.rect;
        if (bounds.width <= 0f || bounds.height <= 0f) return;

        float thickness = Mathf.Min(bounds.width, bounds.height) * edgeWidth;
        const int axisCount = 10;
        for (int y = 0; y < axisCount; y++)
        {
            float vertical = EdgeDistance(y, thickness);
            float positionY = y < 5 ? bounds.yMin + vertical : bounds.yMax - vertical;
            for (int x = 0; x < axisCount; x++)
            {
                float horizontal = EdgeDistance(x, thickness);
                float positionX = x < 5 ? bounds.xMin + horizontal : bounds.xMax - horizontal;
                float distance = Mathf.Min(horizontal, vertical) / thickness;
                Color vertexColor = color;
                vertexColor.a *= 1f - Mathf.SmoothStep(0f, 1f, distance);
                helper.AddVert(new Vector2(positionX, positionY), vertexColor, Vector2.zero);
            }
        }

        for (int y = 0; y < axisCount - 1; y++)
        {
            for (int x = 0; x < axisCount - 1; x++)
            {
                int first = y * axisCount + x;
                helper.AddTriangle(first, first + axisCount, first + 1);
                helper.AddTriangle(first + 1, first + axisCount, first + axisCount + 1);
            }
        }
    }

    private static float EdgeDistance(int index, float thickness)
    {
        int step = index < 5 ? index : 9 - index;
        return FalloffSteps[step] * thickness;
    }
}
