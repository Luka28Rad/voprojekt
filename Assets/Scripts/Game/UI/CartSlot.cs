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
    }
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Dropped in cart: "+cartNumber.ToString());
        if (eventData.pointerDrag != null)
        {
            var dragged = eventData.pointerDrag.transform as RectTransform;
            dragged.anchoredPosition = Vector2.zero;
            if (cartNumber == 1)
                lobby.ChangeLocation(1);
            else if (cartNumber == 2)
                lobby.ChangeLocation(2);
        }
    }
}
    
