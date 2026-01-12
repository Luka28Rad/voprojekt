using Unity.Netcode;
using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    [Header("Player Cards Container")]
    [SerializeField] Transform playerCardsContainer;


    //To be fair ovo vjv nije best way jer se update poziva svaki frame
    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            return;
        }
        //Za svakog klijenta
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            //Uzmi njegove PlayerNetworkData podatke
            var playerNetworkData = client.PlayerObject.GetComponent<PlayerNetworkData>();

            //Pronajdi karticu koja pripada ovom klijentu
            var editLook = FindCardOfOwner(client.ClientId);
                
            //Postavi ispravne indekse za tu karticu
            if(editLook != null){
                editLook.hairBackRenderer.sprite = editLook.hairBackSelect[playerNetworkData.hairBackIndex.Value];
                editLook.hairBackRenderer.SetNativeSize();
                editLook.eyebrowsRenderer.sprite = editLook.eyebrowsSelect[playerNetworkData.eyebrowsIndex.Value];
                editLook.eyebrowsRenderer.SetNativeSize();
                editLook.hairFrontRenderer.sprite = editLook.hairFrontSelect[playerNetworkData.hairFrontIndex.Value];
                editLook.hairFrontRenderer.SetNativeSize();
                editLook.eyesRenderer.sprite = editLook.eyesSelect[playerNetworkData.eyesIndex.Value];
                editLook.eyesRenderer.SetNativeSize();
                editLook.skinRenderer.sprite = editLook.skinSelect[playerNetworkData.skinIndex.Value];
                editLook.skinRenderer.SetNativeSize();
                editLook.outfitRenderer.sprite = editLook.outfitSelect[playerNetworkData.outfitIndex.Value];
                editLook.outfitRenderer.SetNativeSize();
            }
        }
    }

    //Funkcija koja pronajde karticu od klijenta (specificno pronajde EditPlayerLook skriptu jer ima sve renderere u sebi)
    private EditPlayerLook FindCardOfOwner(ulong clientId){
        foreach (Transform child in playerCardsContainer){
            if (child.GetComponent<EditPlayerLook>())
            {
                var editLook = child.GetComponent<EditPlayerLook>();
                if (editLook.linkedClientId == clientId){
                    return editLook;
                }
            }
        }
        return null;
    }

    public Transform GetContainer()
    {
        return playerCardsContainer;
    }

    public void CreatePlayerUI()
    {

    }
}