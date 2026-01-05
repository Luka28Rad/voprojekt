using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EditPlayerLook : MonoBehaviour
{
    [Header("Edit UI")]
    [SerializeField] private GameObject editUI;
    [SerializeField] private Button editButton;
    [SerializeField] private Button doneButton;

    [Header("Appearance elements")]
    [SerializeField] public Image hairBackRenderer;
    [SerializeField] public Image eyebrowsRenderer;
    [SerializeField] public Image hairFrontRenderer;
    [SerializeField] public Image eyesRenderer;
    [SerializeField] public Image skinRenderer;
    [SerializeField] public Image outfitRenderer;

    [Header("Appearance elements selection")]
    [SerializeField] public Sprite[] hairBackSelect;
    [SerializeField] public Sprite[] eyebrowsSelect;
    [SerializeField] public Sprite[] hairFrontSelect;
    [SerializeField] public Sprite[] eyesSelect;
    [SerializeField] public Sprite[] skinSelect;
    [SerializeField] public Sprite[] outfitSelect;

    [SerializeField] public PlayerNetworkData networkData;

    public ulong linkedClientId; //Koji je id klijenta kojem pripada kartica
    private int hairBackIndex, eyebrowsIndex, hairFrontIndex, eyesIndex, skinIndex, outfitIndex; //Indeksi za koristenje pri mjenjaju unutar funkcij
    private bool canEdit = false;

    public void Start(){
        //Indeksi za koristenje pri mjenjaju unutar funkcije, uzima se pocetna vrijednost koja ce vjv biti 0 uvijek
        //Moze se kasnije napraviti da bude random ili neki preseti
        hairBackIndex = networkData.hairBackIndex.Value;
        eyebrowsIndex = networkData.eyebrowsIndex.Value;
        hairFrontIndex = networkData.hairFrontIndex.Value;
        eyesIndex = networkData.eyesIndex.Value;
        skinIndex = networkData.skinIndex.Value;
        outfitIndex = networkData.outfitIndex.Value;
    }
    public ulong getId()
    {
        return linkedClientId;
    }
    //Funkcija za povezivanje kartice sa igraèem
    public void LinkClientId(ulong clientId)
    {
        linkedClientId = clientId;
        if (linkedClientId == NetworkManager.Singleton.LocalClientId){
            canEdit = true;
        }
        editButton.gameObject.SetActive(canEdit);
        doneButton.gameObject.SetActive(false);
        editUI.SetActive(false);
    }

    //Funkcija za otvaranje edit UI
    public void EditLook()
    {
        if (!canEdit) return;
        editUI.SetActive(true);
        doneButton.gameObject.SetActive(true);
        editButton.gameObject.SetActive(false);
    }

    //Funkcija za zatvaranje edit UI
    public void DoneEditing()
    {
        if (!canEdit) return;
        editUI.SetActive(false);
        doneButton.gameObject.SetActive(false);
        editButton.gameObject.SetActive(true);
    }

    //Helper funkcije za paliti/gasiti edit button za in game koristenje
    public void DisableButtons()
    {
        editButton.gameObject.SetActive(false);
        doneButton.gameObject.SetActive(false);
        editUI.SetActive(false);
    }

    //Izbori za izgleda lijevo/desno za kosu,lice,...
    public void HairLeft(){
        ChangeIndex(ref hairBackIndex, hairBackSelect, -1);
        ChangeIndex(ref eyebrowsIndex, eyebrowsSelect, -1);
        ChangeIndex(ref hairFrontIndex, hairFrontSelect, -1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairBackIndex, eyebrowsIndex, hairFrontIndex, eyesIndex, skinIndex, outfitIndex);
        }
    }
    public void HairRight(){
        ChangeIndex(ref hairBackIndex, hairBackSelect, +1);
        ChangeIndex(ref eyebrowsIndex, eyebrowsSelect, +1);
        ChangeIndex(ref hairFrontIndex, hairFrontSelect, +1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairBackIndex, eyebrowsIndex, hairFrontIndex, eyesIndex, skinIndex, outfitIndex);
        }
    }
    public void EyesLeft(){
        ChangeIndex(ref eyesIndex, eyesSelect, -1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairBackIndex, eyebrowsIndex, hairFrontIndex, eyesIndex, skinIndex, outfitIndex);
        }
    }
    public void EyesRight() {
        ChangeIndex(ref eyesIndex, eyesSelect, +1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairBackIndex, eyebrowsIndex, hairFrontIndex, eyesIndex, skinIndex, outfitIndex);
        }
    }
    public void SkinLeft() {
        ChangeIndex(ref skinIndex, skinSelect, -1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairBackIndex, eyebrowsIndex, hairFrontIndex, eyesIndex, skinIndex, outfitIndex);
        }
    }
    public void SkinRight() {
        ChangeIndex(ref skinIndex, skinSelect, +1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairBackIndex, eyebrowsIndex, hairFrontIndex, eyesIndex, skinIndex, outfitIndex);
        }
    }
    public void OutfitLeft() {
        ChangeIndex(ref outfitIndex, outfitSelect, -1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairBackIndex, eyebrowsIndex, hairFrontIndex, eyesIndex, skinIndex, outfitIndex);
        }
    }
    public void OutfitRight() {
        ChangeIndex(ref outfitIndex, outfitSelect, +1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairBackIndex, eyebrowsIndex, hairFrontIndex, eyesIndex, skinIndex, outfitIndex);
        }
    }

    //Rotacija izbora za izgled
    private void ChangeIndex(ref int index, Sprite[] choices, int pomak)
    {
        index = (index + pomak + choices.Length) % choices.Length;
    }

}
