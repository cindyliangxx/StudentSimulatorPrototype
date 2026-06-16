using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private float dragThreshold = 160f;

    private GameManager gameManager;
    private PrototypeHUD hud;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 startPosition;
    private Vector2 pointerStartPosition;
    private bool isDragging;

    public float DragThreshold
    {
        get => dragThreshold;
        set => dragThreshold = Mathf.Max(1f, value);
    }

    public void Initialize(GameManager manager, PrototypeHUD prototypeHUD, Canvas parentCanvas)
    {
        gameManager = manager;
        hud = prototypeHUD;
        canvas = parentCanvas;
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    public void ResetCardPosition()
    {
        if (rectTransform == null)
        {
            return;
        }

        isDragging = false;
        rectTransform.anchoredPosition = startPosition;
        hud?.ClearDragHint();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanDrag())
        {
            return;
        }

        isDragging = true;
        startPosition = rectTransform.anchoredPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pointerStartPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || !CanDrag())
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 pointerCurrentPosition);

        Vector2 delta = pointerCurrentPosition - pointerStartPosition;
        rectTransform.anchoredPosition = startPosition + new Vector2(delta.x, 0f);
        hud?.SetDragHint(delta.x);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        float offsetX = rectTransform.anchoredPosition.x - startPosition.x;

        if (offsetX <= -dragThreshold)
        {
            ResetCardPosition();
            gameManager.ChooseLeft();
            return;
        }

        if (offsetX >= dragThreshold)
        {
            ResetCardPosition();
            gameManager.ChooseRight();
            return;
        }

        ResetCardPosition();
    }

    private bool CanDrag()
    {
        return gameManager != null && !gameManager.IsGameOver && gameManager.CurrentCard != null && canvas != null;
    }
}
