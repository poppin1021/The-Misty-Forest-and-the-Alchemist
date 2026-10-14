using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 재료의 표시용 복사본을 드래그하고, 유효한 영역에 놓으면 재료 이동을 요청한다.
public class TokenDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private BagController bagController;
    private int tokenIndex;
    private bool fromCauldron;
    private RectTransform dropTarget;

    private Button sourceButton;
    private RectTransform sourceRect;
    private GameObject dragPreview;
    private RectTransform previewRect;
    private RectTransform canvasRect;
    private Vector2 dragOffset;
    private bool isDragging;

    private void Awake()
    {
        sourceButton = GetComponent<Button>();
        sourceRect = GetComponent<RectTransform>();
    }

    // BagController가 버튼을 생성할 때 재료 번호, 이동 방향과 대상 영역을 연결한다.
    public void Initialize(
        BagController controller,
        int index,
        bool isFromCauldron,
        RectTransform target)
    {
        bagController = controller;
        tokenIndex = index;
        fromCauldron = isFromCauldron;
        dropTarget = target;
    }

    // 드래그 시작: 원본은 레이아웃에 남겨 두고 복사본만 Canvas 위로 움직인다.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left
            || !sourceButton.interactable
            || bagController == null
            || !bagController.CanDragTokens())
        {
            return;
        }

        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        canvasRect = rootCanvas.transform as RectTransform;

        dragPreview = Instantiate(gameObject, canvasRect, true);
        dragPreview.name = "TokenDragPreview";
        previewRect = dragPreview.GetComponent<RectTransform>();
        previewRect.position = sourceRect.position;
        previewRect.SetAsLastSibling();

        dragPreview.GetComponent<TokenDragHandler>().enabled = false;
        dragPreview.GetComponent<Button>().enabled = false;
        CanvasGroup previewGroup = dragPreview.GetComponent<CanvasGroup>();
        if (previewGroup == null)
        {
            previewGroup = dragPreview.AddComponent<CanvasGroup>();
        }
        // 복사본이 드롭 영역의 마우스 입력을 가로막지 않게 한다.
        previewGroup.blocksRaycasts = false;
        previewGroup.interactable = false;

        isDragging = true;
        sourceButton.interactable = false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera,
            out Vector2 pointerPosition))
        {
            ClearDragPreview();
            return;
        }

        dragOffset = (Vector2)previewRect.localPosition - pointerPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragPreview == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera,
            out Vector2 pointerPosition))
        {
            Vector2 nextPosition = pointerPosition + dragOffset;
            previewRect.localPosition = new Vector3(
                nextPosition.x, nextPosition.y, previewRect.localPosition.z);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || dragPreview == null)
        {
            return;
        }

        bool droppedOnTarget = dropTarget != null
            && RectTransformUtility.RectangleContainsScreenPoint(
                dropTarget, eventData.position, eventData.pressEventCamera);

        // 이동 후 원본 버튼들이 다시 생성되므로 복사본부터 정리한다.
        ClearDragPreview();

        if (droppedOnTarget)
        {
            bagController.TryMoveTokenByDrag(tokenIndex, fromCauldron);
        }
    }

    // 턴 전환 등으로 원본이 비활성화돼도 드래그 복사본이 남지 않게 한다.
    private void OnDisable()
    {
        if (isDragging)
        {
            ClearDragPreview();
        }
    }

    // 복사본을 정리하고 현재 전투 상태에 맞춰 원본 버튼의 입력을 복원한다.
    private void ClearDragPreview()
    {
        isDragging = false;
        if (dragPreview != null)
        {
            dragPreview.SetActive(false);
            Destroy(dragPreview);
            dragPreview = null;
            previewRect = null;
        }
        if (sourceButton != null && bagController != null)
        {
            sourceButton.interactable = bagController.CanDragTokens();
        }
    }
}
