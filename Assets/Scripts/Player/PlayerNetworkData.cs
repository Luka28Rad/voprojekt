using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class PlayerNetworkData : NetworkBehaviour
{
    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(true); // ako je igrac ziv onda je true
    public NetworkVariable<ulong> NightTargetId = new NetworkVariable<ulong>(ulong.MaxValue);
    public NetworkVariable<int> noPlayers = new NetworkVariable<int>(0);
    public NetworkVariable<bool> hasWon = new NetworkVariable<bool>(false);
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<int> TrainCart = new NetworkVariable<int>(0); // 0 = no assigned cart
    public NetworkVariable<int> hairBackIndex = new NetworkVariable<int>();
    public NetworkVariable<int> eyebrowsIndex = new NetworkVariable<int>();
    public NetworkVariable<int> hairFrontIndex = new NetworkVariable<int>();
    public NetworkVariable<int> eyesIndex = new NetworkVariable<int>();
    public NetworkVariable<int> skinIndex = new NetworkVariable<int>();
    public NetworkVariable<int> outfitIndex = new NetworkVariable<int>();
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

    public bool CanInteractWith(PlayerNetworkData target)
    {
        if (!target) return false;
        if (target == this && MyRole != PlayerRole.Doctor) return false;
        if (!IsAlive.Value) return false;
        if (!target.IsAlive.Value) return false;

        return TrainCart.Value == target.TrainCart.Value;
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(string name)
    {
        PlayerName.Value = name;
    }
    
    [ServerRpc]
    public void SetNightTargetServerRpc(ulong targetId)
    {
        NightTargetId.Value = targetId;
        Debug.Log($"Server: Player {OwnerClientId} {MyRole} targeted Object {targetId}");
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
        Debug.Log($"-----My role is: {MyRole}");
        OnRoleAssigned?.Invoke(MyRole);
    }

    [ClientRpc]
    public void ReceiveNotificationClientRpc(string message, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[GAME EVENT]: {message}");
    }
}