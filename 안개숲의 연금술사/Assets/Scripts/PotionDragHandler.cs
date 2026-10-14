using UnityEngine;
using UnityEngine.EventSystems;

// 완성된 물약을 드래그해 적 또는 플레이어에게 사용한다.
public class PotionDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private BagController bagController;
    [SerializeField] private RectTransform enemyTarget;
    [SerializeField] private RectTransform playerTarget;

    private RectTransform potionRect;
    private RectTransform parentRect;

    private Vector3 startPosition;
    private Vector2 dragOffset;
    private bool isDragging;

    private void Awake()
    {
        potionRect = GetComponent<RectTransform>();
        parentRect = transform.parent.GetComponent<RectTransform>();
    }

    // 원래 위치와 잡은 지점의 간격을 저장해 드래그 중 갑자기 위치가 바뀌지 않게 한다.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        startPosition = potionRect.localPosition;

        isDragging = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 pointerPosition
        );

        if (!isDragging)
        {
            return;
        }

        dragOffset = (Vector2)startPosition - pointerPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 pointerPosition))
        {
            Vector2 nextPosition = pointerPosition + dragOffset;

            potionRect.localPosition = new Vector3(
                nextPosition.x,
                nextPosition.y,
                startPosition.z
            );
        }
    }

    // 물약을 원래 자리로 돌린 뒤, 놓은 영역에 맞게 사용을 요청한다.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        potionRect.localPosition = startPosition;

        bool droppedOnEnemy =
            RectTransformUtility.RectangleContainsScreenPoint(
                enemyTarget,
                eventData.position,
                eventData.pressEventCamera
            );

        bool droppedOnPlayer =
            RectTransformUtility.RectangleContainsScreenPoint(
                playerTarget,
                eventData.position,
                eventData.pressEventCamera
            );

        if (droppedOnEnemy)
        {
            bagController.UsePreparedPotionOnEnemy();
        }
        else if (droppedOnPlayer)
        {
            bagController.UsePreparedPotionOnSelf();
        }
    }
}
