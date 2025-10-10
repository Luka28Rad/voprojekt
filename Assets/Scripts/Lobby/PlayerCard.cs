using TMPro;
using UnityEngine;

//kartice za lobby, trebat ce dodati isto image al cemo vidit kako cemo to rijesit, oce li biti character creation ili premade characters
public class PlayerCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;

    public void SetPlayerName(string name)
    {
        nameText.text = name;
    }
}