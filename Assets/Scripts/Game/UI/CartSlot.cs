using UnityEngine;
using UnityEngine.EventSystems;

public class CartSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private LobbyManager lobby;
    private int cartNumber = 0;
    private void Awake()
    {
        string name = gameObject.name;
        if (name == "TrainCart1")
            cartNumber = 1;
        else if (name == "TrainCart2")
            cartNumber = 2;
        else if (name == "TrainCart3")
            cartNumber = 3;
    }
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Dropped in cart: "+cartNumber.ToString());
        if (eventData.pointerDrag != null)
        {
            var drag= eventData.pointerDrag.GetComponent<DragDrop>();
            if (drag != null) drag.SetDropValid(true);
            var draggedRT = eventData.pointerDrag.GetComponent<RectTransform>();
            draggedRT.SetParent(transform, false);
            draggedRT.anchoredPosition = Vector2.zero;
            lobby.ChangeLocation(cartNumber);
        }
    }
}
    
