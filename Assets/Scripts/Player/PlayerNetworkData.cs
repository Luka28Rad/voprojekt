using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class PlayerNetworkData : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<int> TrainCart = new NetworkVariable<int>(0); // 0 = no assigned cart
    public NetworkVariable<int> hairBackIndex = new NetworkVariable<int>();
    public NetworkVariable<int> eyebrowsIndex = new NetworkVariable<int>();
    public NetworkVariable<int> hairFrontIndex = new NetworkVariable<int>();
    public NetworkVariable<int> eyesIndex = new NetworkVariable<int>();
    public NetworkVariable<int> skinIndex = new NetworkVariable<int>();
    public NetworkVariable<int> outfitIndex = new NetworkVariable<int>();
    public NetworkVariable<bool> dead = new NetworkVariable<bool>(false); //Ako je igrac izglasan van ili ubijen onda je true
    public PlayerRole MyRole { get; private set; } = PlayerRole.Unassigned;
    public bool PlayerIsAlive { get; private set; } = true;
    public event Action<PlayerRole> OnRoleAssigned;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string name = LobbyManager.PlayerName ?? "Player";
            SetPlayerNameServerRpc(name);
        }

        PlayerName.OnValueChanged += OnNameChanged;
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnNameChanged;
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(string name)
    {
        PlayerName.Value = name;
    }

    //Postavljanje indeksa u PlayerNetworkData kako bi skripta PlayerUIManager mogla uzeti te indekse
    [ServerRpc]
    public void SetAppearanceServerRpc(int hairBack, int eyebrows, int hairFront, int eyes, int skin, int outfit){
        hairBackIndex.Value = hairBack;
        eyebrowsIndex.Value = eyebrows;
        hairFrontIndex.Value = hairFront;
        eyesIndex.Value = eyes;
        skinIndex.Value = skin;
        outfitIndex.Value = outfit;
    }
    [ServerRpc]
    public void SetLocationServerRpc(int cart)
    {
        TrainCart.Value = cart;
    }

    private void OnNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.UpdateLobbyUI();
        }
    }

    [ClientRpc]
    public void SetRoleClientRpc(PlayerRole assignedRole, ClientRpcParams clientRpcParams = default)
    {
        MyRole = assignedRole;
        Debug.Log($"My role is: {MyRole}");
        OnRoleAssigned?.Invoke(MyRole);
    }
}