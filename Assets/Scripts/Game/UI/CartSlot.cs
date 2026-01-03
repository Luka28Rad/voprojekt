using UnityEngine;
using UnityEngine.EventSystems;

public class CartSlot : MonoBehaviour, IDropHandler
{
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
            eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition = GetComponent<RectTransform>().anchoredPosition;
        }

    }
}
    
