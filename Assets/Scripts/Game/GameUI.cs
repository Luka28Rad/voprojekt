using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roleDisplayText;

    private void OnEnable()
    {
        if (NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            SubscribeToRoleEvent();
        }
        else
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            var localPlayerData = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkData>();
            if (localPlayerData != null)
            {
                localPlayerData.OnRoleAssigned -= DisplayRole;
            }
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SubscribeToRoleEvent();
        }
    }

    private void SubscribeToRoleEvent()
    {
        var localPlayerData = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkData>();
        localPlayerData.OnRoleAssigned += DisplayRole;

        if (localPlayerData.MyRole != PlayerRole.Unassigned)
        {
            DisplayRole(localPlayerData.MyRole);
        }
    }

    private void DisplayRole(PlayerRole role)
    {
        roleDisplayText.text = $"Your Role: {role}";
    }
}