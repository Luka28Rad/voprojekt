using UnityEngine;

public class TransparentBackground : MonoBehaviour
{
    private void Awake()
    {
        EditPlayerLook[] allCards = FindObjectsByType<EditPlayerLook>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var card in allCards) {
            Transform backgroundTransform = card.transform.GetChild(0);
            UnityEngine.UI.Image backgroundImage = backgroundTransform.GetComponent<UnityEngine.UI.Image>();
            Color c = backgroundImage.color;
            c.a = 0f;
            backgroundImage.color = c;
        }
    }
}
