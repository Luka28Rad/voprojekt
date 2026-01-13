using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ActionResolutionSystem : NetworkBehaviour
{
    public static ActionResolutionSystem Instance { get; private set; }
    public List<ulong> LastNightVictims = new List<ulong>(); 

    private void Awake()
    {
        Instance = this;
    }

    public void ResolveTurn()
    {
        if (!IsServer) return;

        var allPlayers = FindObjectsByType<PlayerNetworkData>(FindObjectsSortMode.None);
        Dictionary<ulong, PlayerNetworkData> playerMap = new Dictionary<ulong, PlayerNetworkData>();
        foreach (var p in allPlayers) playerMap[p.NetworkObjectId] = p;

        HashSet<ulong> blockedPlayerIds = new HashSet<ulong>();
        HashSet<ulong> protectedTargetIds = new HashSet<ulong>();
        List<ulong> playersToKill = new List<ulong>();

        // --- IMPOSTOR CONTROLLER ---
        foreach (var player in allPlayers)
        {
            if (!player.IsAlive.Value) continue;
            
            PlayerRole role = GetServerSideRole(player.OwnerClientId);
            ulong targetId = player.NightTargetId.Value;

            if (role == PlayerRole.ImpostorControl && targetId != ulong.MaxValue)
            {
                if (playerMap.ContainsKey(targetId))
                {
                    blockedPlayerIds.Add(targetId);
                    Debug.Log($"Controller {player.NetworkObjectId} BLOCKED Player {targetId}");
                }
            }
        }

        // --- DOCTOR ---
        foreach (var player in allPlayers)
        {
            if (!player.IsAlive.Value) continue;
            if (blockedPlayerIds.Contains(player.NetworkObjectId)) continue; // skip ako blokan

            PlayerRole role = GetServerSideRole(player.OwnerClientId);
            ulong targetId = player.NightTargetId.Value;

            if (role == PlayerRole.Doctor && targetId != ulong.MaxValue)
            {
                if (playerMap.ContainsKey(targetId))
                {
                    protectedTargetIds.Add(targetId);
                    Debug.Log($"Doctor {player.NetworkObjectId} PROTECTING {targetId}");
                }
            }
        }

        // --- IMPOSTOR ---
        foreach (var player in allPlayers)
        {
            if (!player.IsAlive.Value) continue;
            
            PlayerRole role = GetServerSideRole(player.OwnerClientId);
            ulong targetId = player.NightTargetId.Value;
            
            if (role == PlayerRole.Impostor && targetId != ulong.MaxValue)
            {
                if (protectedTargetIds.Contains(targetId))
                {
                    Debug.Log($"Attack on {targetId} BLOCKED by Doctor.");
                    SendPrivateMsg(player.OwnerClientId, "Your target was saved by a Doctor.");
                }
                else
                {
                    Debug.Log($"Killed {targetId}");
                    if (playerMap.ContainsKey(targetId))
                        playersToKill.Add(targetId);
                }
            }
        }

        // --- INVESTIGATOR ---
        foreach (var player in allPlayers)
        {
            if (!player.IsAlive.Value) continue;
            if (blockedPlayerIds.Contains(player.NetworkObjectId))
            {
                SendPrivateMsg(player.OwnerClientId, "You were blocked and gathered no info.");
                continue;
            }

            PlayerRole role = GetServerSideRole(player.OwnerClientId);
            ulong targetId = player.NightTargetId.Value;

            if (role == PlayerRole.Detective && targetId != ulong.MaxValue && playerMap.ContainsKey(targetId))
            {
                PlayerRole targetRole = GetServerSideRole(playerMap[targetId].OwnerClientId);
                
                bool isSuspicious = targetRole is PlayerRole.Impostor or PlayerRole.ImpostorControl;
                string clue = isSuspicious ? "<color=red>SUSPICIOUS</color>" : "<color=green>INNOCENT</color>";

                SendPrivateMsg(player.OwnerClientId, $"Investigation Result: Target is {clue}.");
            }
        }

        // --- DEATHS ---
        HashSet<ulong> uniqueVictims = new HashSet<ulong>(playersToKill);

        foreach (var victimId in uniqueVictims)
        {
            if (playerMap.TryGetValue(victimId, out PlayerNetworkData victim))
            {
                victim.IsAlive.Value = false;
            }
        }
        
        ulong[] deadPlayerIds = uniqueVictims.ToArray();
        SyncDeathsClientRpc(deadPlayerIds);

        // --- RESET TARGETS ---
        foreach (var player in allPlayers)
        {
            player.NightTargetId.Value = ulong.MaxValue;
        }
        
        GameManager.Instance.CheckWinCondition(true);
    }
    
    [ClientRpc]
    private void SyncDeathsClientRpc(ulong[] deadPlayerIds)
    {
        LastNightVictims.Clear();

        foreach(ulong id in deadPlayerIds)
        {
            LastNightVictims.Add(id);
            Debug.Log($"[Client] Received death confirmation for: {id}");
        }

        if (deadPlayerIds.Length > 0)
        {
            Debug.Log($"[Client] {deadPlayerIds.Length} players died.");
        }
        else
        {
            Debug.Log("[Client] Peaceful night.");
        }
    }

    private PlayerRole GetServerSideRole(ulong clientId)
    {
        if (GameManager.Instance && GameManager.Instance.ServerRoleMap.ContainsKey(clientId))
        {
            return GameManager.Instance.ServerRoleMap[clientId];
        }

        return PlayerRole.Unassigned; 
    }

    private void SendPrivateMsg(ulong targetClientId, string msg)
    {
        ClientRpcParams p = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } } };
        MessageClientRpc(msg, p);
    }

    [ClientRpc]
    private void MessageClientRpc(string msg, ClientRpcParams p = default)
    {
        // notif todo
        PlayerNetworkData localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkData>();
        localPlayer.ReceiveNotificationClientRpc(msg); 
    }

    [ClientRpc]
    private void MessageAllClientsClientRpc(string msg)
    {
         // Notif todo
         Debug.Log($"[SERVER BROADCAST]: {msg}");
    }
}