using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkData : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    public PlayerRole MyRole { get; private set; } = PlayerRole.Unassigned;
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