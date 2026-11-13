using UnityEngine;
using UnityEngine.UI;

// 별도 이미지 없이 얇은 충격파와 빛 조각을 그린다.
public class CombatImpactGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        const int segments = 40;
        Vector2 center = rectTransform.rect.center;
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
        float innerRadius = radius * 0.89f;

        for (int i = 0; i < segments; i++)
        {
            float angleA = i * Mathf.PI * 2f / segments;
            float angleB = (i + 1) * Mathf.PI * 2f / segments;
            Vector2 directionA = new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA));
            Vector2 directionB = new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB));
            int first = helper.currentVertCount;
            helper.AddVert(center + directionA * innerRadius, color, Vector2.zero);
            helper.AddVert(center + directionA * radius, color, Vector2.zero);
            helper.AddVert(center + directionB * radius, color, Vector2.zero);
            helper.AddVert(center + directionB * innerRadius, color, Vector2.zero);
            helper.AddTriangle(first, first + 1, first + 2);
            helper.AddTriangle(first, first + 2, first + 3);
        }
    }
}
