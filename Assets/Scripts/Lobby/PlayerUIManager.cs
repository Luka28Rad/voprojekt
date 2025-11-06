using Unity.Netcode;
using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    [Header("Player Cards Container")]
    [SerializeField] private Transform playerCardsContainer;

    //To be fair ovo vjv nije best way jer se update poziva svaki frame
    private void Update()
    {
        //Za svakog klijenta
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            //Uzmi njegove PlayerNetworkData podatke
            var playerNetworkData = client.PlayerObject.GetComponent<PlayerNetworkData>();

            //Pronajdi karticu koja pripada ovom klijentu
            var editLook = FindCardOfOwner(client.ClientId);
                
            //Postavi ispravne indekse za tu karticu
            if(editLook != null){
                editLook.hairRenderer.sprite = editLook.hairSelect[playerNetworkData.hairIndex.Value];
                editLook.headRenderer.sprite = editLook.headSelect[playerNetworkData.headIndex.Value];
                editLook.faceRenderer.sprite = editLook.faceSelect[playerNetworkData.faceIndex.Value];
                editLook.bodyRenderer.sprite = editLook.bodySelect[playerNetworkData.bodyIndex.Value];
                editLook.legsRenderer.sprite = editLook.legsSelect[playerNetworkData.legsIndex.Value];
            }
        }
    }

    //Funkcija koja pronajde karticu od klijenta (specificno pronajde EditPlayerLook skriptu jer ima sve renderere u sebi)
    private EditPlayerLook FindCardOfOwner(ulong clientId){
        foreach (Transform child in playerCardsContainer){
            var editLook = child.GetComponent<EditPlayerLook>();
            if (editLook.linkedClientId == clientId){
                return editLook;
            }
        }
        return null;
    }
}