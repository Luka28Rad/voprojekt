using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInteractionButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button interactButton;
    [SerializeField] private TMP_Text buttonText;

    public PlayerNetworkData targetPlayer; //player na kojem se nalazi gumb
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        interactButton.onClick.AddListener(OnButtonClicked);

        interactButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        ManageVisibility();
    }

    private void ManageVisibility()
    {
        if (!NetworkManager.Singleton || NetworkManager.Singleton.LocalClient == null || !targetPlayer) return;
        if (!GameUI.Instance) return;
        
        var localPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayerObject == null) return;
        var localData = localPlayerObject.GetComponent<PlayerNetworkData>();

        // --- IS IT NIGHT ---
        bool isNight = GameUI.Instance.nightTimeScreen.activeInHierarchy;
        if (!isNight)
        {
            interactButton.gameObject.SetActive(false);
            return;
        }

        // --- AM I ALIVE & DO I HAVE A ROLE ---
        if (!localData.IsAlive.Value || localData.MyRole == PlayerRole.Villager || localData.MyRole == PlayerRole.Fool)
        {
            interactButton.gameObject.SetActive(false);
            return;
        }

        // --- IS TARGET VALID ---
        bool canInteract = localData.CanInteractWith(targetPlayer);

        // doktor gumb
        if (targetPlayer.NetworkObjectId == localData.NetworkObjectId && localData.MyRole != PlayerRole.Doctor)
        {
            canInteract = false;
        }

        interactButton.gameObject.SetActive(canInteract);
        
        if (canInteract && buttonText)
        {
            if (localData.MyRole == PlayerRole.Impostor) buttonText.text = "KILL";
            else if (localData.MyRole == PlayerRole.ImpostorControl) buttonText.text = "BLOCK";
            else if (localData.MyRole == PlayerRole.Doctor) buttonText.text = "PROTECT";
            else if (localData.MyRole == PlayerRole.Investigator) buttonText.text = "INVESTIGATE";
            else buttonText.text = "INTERACT";
        }
    }

    private void OnButtonClicked()
    {
        var localPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        var localData = localPlayerObject.GetComponent<PlayerNetworkData>();

        if (localData != null)
        {
            Debug.Log($"Clicked button on: {targetPlayer.PlayerName.Value}");
            localData.SetNightTargetServerRpc(targetPlayer.NetworkObjectId);
        }
    }
}