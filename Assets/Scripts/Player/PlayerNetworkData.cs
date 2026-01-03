using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class PlayerNetworkData : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<int> TrainCart = new NetworkVariable<int>(0); // 0 = no assigned cart
    public NetworkVariable<int> hairIndex = new NetworkVariable<int>();
    public NetworkVariable<int> headIndex = new NetworkVariable<int>();
    public NetworkVariable<int> faceIndex = new NetworkVariable<int>();
    public NetworkVariable<int> bodyIndex = new NetworkVariable<int>();
    public NetworkVariable<int> legsIndex = new NetworkVariable<int>();
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
    public void SetAppearanceServerRpc(int hair, int head, int face, int body, int legs){
        hairIndex.Value = hair;
        headIndex.Value = head;
        faceIndex.Value = face;
        bodyIndex.Value = body;
        legsIndex.Value = legs;
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