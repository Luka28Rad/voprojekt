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
    [SerializeField] public Image hairRenderer;
    [SerializeField] public Image headRenderer;
    [SerializeField] public Image faceRenderer;
    [SerializeField] public Image bodyRenderer;
    [SerializeField] public Image legsRenderer;

    [Header("Appearance elements selection")]
    [SerializeField] public Sprite[] hairSelect;
    [SerializeField] public Sprite[] headSelect;
    [SerializeField] public Sprite[] faceSelect;
    [SerializeField] public Sprite[] bodySelect;
    [SerializeField] public Sprite[] legsSelect;

    [SerializeField] public PlayerNetworkData networkData;

    public ulong linkedClientId; //Koji je id klijenta kojem pripada kartica
    private int hairIndex, headIndex, faceIndex, bodyIndex, legsIndex; //Indeksi za koristenje pri mjenjaju unutar funkcij
    private bool canEdit = false;

    public void Start(){
        //Indeksi za koristenje pri mjenjaju unutar funkcije, uzima se pocetna vrijednost koja ce vjv biti 0 uvijek
        //Moze se kasnije napraviti da bude random ili neki preseti
        hairIndex = networkData.hairIndex.Value;
        headIndex = networkData.headIndex.Value;
        faceIndex = networkData.faceIndex.Value;
        bodyIndex = networkData.bodyIndex.Value;
        legsIndex = networkData.legsIndex.Value;
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
        ChangeIndex(ref hairIndex, hairSelect, -1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairIndex, headIndex, faceIndex, bodyIndex, legsIndex);
        }
    }
    public void HairRight(){
        ChangeIndex(ref hairIndex, hairSelect, +1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairIndex, headIndex, faceIndex, bodyIndex, legsIndex);
        }
    }
    public void HeadLeft(){
        ChangeIndex(ref headIndex, headSelect, -1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairIndex, headIndex, faceIndex, bodyIndex, legsIndex);
        }
    }
    public void HeadRight() {
        ChangeIndex(ref headIndex, headSelect, +1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairIndex, headIndex, faceIndex, bodyIndex, legsIndex);
        }
    }
    public void FaceLeft() {
        ChangeIndex(ref faceIndex, faceSelect, -1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairIndex, headIndex, faceIndex, bodyIndex, legsIndex);
        }
    }
    public void FaceRight() {
        ChangeIndex(ref faceIndex, faceSelect, +1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairIndex, headIndex, faceIndex, bodyIndex, legsIndex);
        }
    }
    public void BodyLeft() {
        ChangeIndex(ref bodyIndex, bodySelect, -1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairIndex, headIndex, faceIndex, bodyIndex, legsIndex);
        }
    }
    public void BodyRight() {
        ChangeIndex(ref bodyIndex, bodySelect, +1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairIndex, headIndex, faceIndex, bodyIndex, legsIndex);
        }
    }
    public void LegsLeft() {
        ChangeIndex(ref legsIndex, legsSelect, -1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairIndex, headIndex, faceIndex, bodyIndex, legsIndex);
        }
    }
    public void LegsRight() {
        ChangeIndex(ref legsIndex, legsSelect, +1);
        if (networkData != null){
            networkData.SetAppearanceServerRpc(hairIndex, headIndex, faceIndex, bodyIndex, legsIndex);
        }
    }

    //Rotacija izbora za izgled
    private void ChangeIndex(ref int index, Sprite[] choices, int pomak)
    {
        index = (index + pomak + choices.Length) % choices.Length;
    }

}
