using UnityEngine;
using UnityEngine.EventSystems;

public class DragDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler
{
    [SerializeField] private GameUI GameUI;
    [SerializeField] private Canvas canvas;
    private RectTransform rt;
    private CanvasGroup canvasGroup;
    
    private Transform ogParent;
    private Vector2 originalAnchoredPos;
    private bool validDrop;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        if (GameUI.dayTimeScreen.activeInHierarchy)
        {
            validDrop = false;
            ogParent = rt.parent;
            originalAnchoredPos = rt.anchoredPosition;
            canvasGroup.alpha = 0.8f;
            Debug.Log("Started drag!");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (GameUI.dayTimeScreen.activeInHierarchy)
        {
            //Debug.Log("Dragging...");
            rt.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        if (GameUI.dayTimeScreen.activeInHierarchy)
        {
            canvasGroup.alpha = 1f;
            //Debug.Log("Ended Drag!");
            if (!validDrop)
            {
                rt.SetParent(ogParent);
                rt.anchoredPosition = originalAnchoredPos;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameUI.dayTimeScreen.activeInHierarchy)
        {
            Debug.Log("Pressed!");
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }

    public void SetDropValid(bool state)
    {
        validDrop = state;
    }
}
