using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ComicRevealLayer : MaskableGraphic
{
    [Header("대각선 경계: X는 위쪽, Y는 아래쪽 위치")]
    [SerializeField]
    private Vector2 villageBorder =
        new Vector2(0.680f, 0.543f);

    [SerializeField]
    private Vector2 forestFirstBorder =
        new Vector2(0.310f, 0.216f);

    [SerializeField]
    private Vector2 forestSecondBorder =
        new Vector2(0.788f, 0.695f);

    [SerializeField]
    private Vector2 preparationBorder =
        new Vector2(0.417f, 0.324f);

    private Image sourceImage;

    private Vector2[] borders =
    {
        Vector2.zero,
        Vector2.one
    };

    private readonly float[] revealAmounts = new float[3];

    public int PanelCount => borders.Length - 1;

    // 페이지에 맞는 경계를 설정하고 모든 컷을 가린다.
    public void PreparePage(int pageIndex, Image image)
    {
        sourceImage = image;
        raycastTarget = false;

        switch (pageIndex)
        {
            case 0:
                borders = new[]
                {
                    Vector2.zero,
                    villageBorder,
                    Vector2.one
                };
                break;

            case 1:
                borders = new[]
                {
                    Vector2.zero,
                    forestFirstBorder,
                    forestSecondBorder,
                    Vector2.one
                };
                break;

            default:
                borders = new[]
                {
                    Vector2.zero,
                    preparationBorder,
                    Vector2.one
                };
                break;
        }

        for (int i = 0; i < revealAmounts.Length; i++)
        {
            revealAmounts[i] = 0f;
        }

        canvasRenderer.cull = false;
        canvasRenderer.SetAlpha(1f);
        SetAllDirty();
    }

    // 0은 완전히 가림, 1은 완전히 표시한 상태다.
    public void SetReveal(int panelIndex, float amount)
    {
        if (panelIndex < 0 || panelIndex >= PanelCount)
        {
            return;
        }

        revealAmounts[panelIndex] = Mathf.Clamp01(amount);
        SetVerticesDirty();
    }

    // 각 컷의 대각선 경계에 맞춰 검은 사각형을 그린다.
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (sourceImage == null || sourceImage.sprite == null)
        {
            return;
        }

        Rect imageRect = GetImageRect();

        if (imageRect.width <= 0f || imageRect.height <= 0f)
        {
            return;
        }

        for (int i = 0; i < PanelCount; i++)
        {
            float alpha = 1f - revealAmounts[i];

            if (alpha <= 0f)
            {
                continue;
            }

            Color32 coverColor =
                new Color(0f, 0f, 0f, alpha);

            int startIndex = vh.currentVertCount;

            vh.AddVert(
                ToLocalPoint(imageRect, borders[i].y, 0f),
                coverColor,
                Vector2.zero
            );

            vh.AddVert(
                ToLocalPoint(imageRect, borders[i].x, 1f),
                coverColor,
                Vector2.zero
            );

            vh.AddVert(
                ToLocalPoint(imageRect, borders[i + 1].x, 1f),
                coverColor,
                Vector2.zero
            );

            vh.AddVert(
                ToLocalPoint(imageRect, borders[i + 1].y, 0f),
                coverColor,
                Vector2.zero
            );

            vh.AddTriangle(
                startIndex,
                startIndex + 1,
                startIndex + 2
            );

            vh.AddTriangle(
                startIndex + 2,
                startIndex + 3,
                startIndex
            );
        }
    }

    // Preserve Aspect로 생기는 여백을 반영한다.
    private Rect GetImageRect()
    {
        Rect rect = sourceImage.GetPixelAdjustedRect();

        if (!sourceImage.preserveAspect
            || rect.width <= 0f
            || rect.height <= 0f)
        {
            return rect;
        }

        Vector2 spriteSize = sourceImage.sprite.rect.size;
        float spriteRatio = spriteSize.x / spriteSize.y;
        float rectRatio = rect.width / rect.height;
        Vector2 pivot = sourceImage.rectTransform.pivot;

        if (spriteRatio > rectRatio)
        {
            float oldHeight = rect.height;
            rect.height = rect.width / spriteRatio;
            rect.y += (oldHeight - rect.height) * pivot.y;
        }
        else
        {
            float oldWidth = rect.width;
            rect.width = rect.height * spriteRatio;
            rect.x += (oldWidth - rect.width) * pivot.x;
        }

        return rect;
    }

    private Vector3 ToLocalPoint(Rect rect, float x, float y)
    {
        Vector3 imagePoint = new Vector3(
            Mathf.Lerp(rect.xMin, rect.xMax, x),
            Mathf.Lerp(rect.yMin, rect.yMax, y),
            0f
        );

        Vector3 worldPoint =
            sourceImage.rectTransform.TransformPoint(imagePoint);

        return rectTransform.InverseTransformPoint(worldPoint);
    }
}