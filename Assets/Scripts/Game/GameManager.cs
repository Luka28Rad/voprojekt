using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

// enum za role
public enum PlayerRole
{
    Unassigned,
    Villager,
    Detective,
    Doctor,
    Impostor,
    ImpostorControl,
    Fool
}

public enum WinTeam
{
    Town,
    Impostors,
    Fool
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public Dictionary<ulong, PlayerRole> ServerRoleMap = new Dictionary<ulong, PlayerRole>();
    
    public NetworkVariable<bool> IsGameOver = new NetworkVariable<bool>(false);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // "Start game" gumb zove ovu metodu, samo ju host moze zvati
    public void StartGameAndAssignRoles()
    {
        // provjera da je host
        if (!IsHost) return;
        
        IsGameOver.Value = false;
        
        // Ids svih igraca
        List<ulong> playerIds = NetworkManager.Singleton.ConnectedClients.Keys.ToList();
        
        // random redoslijed
        Shuffle(playerIds);

        // Dicitonary za role
        ServerRoleMap.Clear();
        
        // prvi je impostor, ak je 6-8 igraca +1 impostor control, 9-10 su +2 impostor control
        int numOfImpostors = 1;
        ServerRoleMap[playerIds[0]] = PlayerRole.Impostor;
		if(playerIds.Count > 8)
        {
            ServerRoleMap[playerIds[1]] = PlayerRole.ImpostorControl;
            ServerRoleMap[playerIds[2]] = PlayerRole.ImpostorControl;
            numOfImpostors = 3;
        }
        else if (playerIds.Count > 5)
        {
            ServerRoleMap[playerIds[1]] = PlayerRole.ImpostorControl;
            numOfImpostors = 2;
        }

        // Ostali su villageri i random role sa sansom
        for (int i = numOfImpostors; i < playerIds.Count; i++)
        {
            float randomChance = Random.Range(0f, 1f);
            if (randomChance < 0.25f)
            {
                ServerRoleMap[playerIds[i]] = PlayerRole.Detective;
            }
            else if (randomChance < 0.5f)
            {
                ServerRoleMap[playerIds[i]] = PlayerRole.Doctor;
            }
			else if (randomChance < 0.6f)
            {
                ServerRoleMap[playerIds[i]] = PlayerRole.Fool;
            }
            else
            {
                ServerRoleMap[playerIds[i]] = PlayerRole.Villager;
            }
        }

        // Salje role playerima privatno
        foreach (var entry in ServerRoleMap)
        {
            ulong clientId = entry.Key;
            PlayerRole role = entry.Value;

            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            var playerData = playerObject.GetComponent<PlayerNetworkData>();
            
            playerData.hasWon.Value = false; 
            playerData.IsAlive.Value = true;
            
            // Ovo salje RPC specificnom klijentu
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            };

            playerData.SetRoleClientRpc(role, clientRpcParams);
            LobbyManager.Instance.RoleShowcaseParamsClientRpc(role, clientRpcParams);
        }
        
    }
    
    public void ExilePlayer(ulong clientId)
    {
        if (!IsServer) return;
        Debug.Log($"[GameManager] Exiled ({clientId})");
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
            data.IsAlive.Value = false;
        }

        PlayerRole role = ServerRoleMap.ContainsKey(clientId) ? ServerRoleMap[clientId] : PlayerRole.Unassigned;
        
        if (role == PlayerRole.Fool)
        {
            Debug.Log($"[GameManager] The Fool ({clientId}) was voted out! Fool wins.");
            EndGame(WinTeam.Fool, clientId, waitForNewspaper: false);
            return;
        }

        CheckWinCondition(waitForNewspaper: false);
    }
    
    public void CheckWinCondition(bool waitForNewspaper = false)
    {
        if (!IsServer) return;

        int impostorsAlive = 0;
        int townAlive = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
            if (data.IsAlive.Value)
            {
                PlayerRole role = ServerRoleMap.ContainsKey(client.ClientId) ? ServerRoleMap[client.ClientId] : PlayerRole.Unassigned;

                if (role == PlayerRole.Impostor || role == PlayerRole.ImpostorControl)
                {
                    impostorsAlive++;
                }
                else if (role != PlayerRole.Unassigned)
                {
                    townAlive++;
                }
            }
        }
        Debug.Log($"[GameManager] There are {impostorsAlive} impostors and {townAlive} people");
        if (impostorsAlive == 0)
        {
            EndGame(WinTeam.Town, ulong.MaxValue, waitForNewspaper);
        }
        else if (impostorsAlive >= townAlive)
        {
            EndGame(WinTeam.Impostors, ulong.MaxValue, waitForNewspaper);
        }
    }
    
    private void EndGame(WinTeam winningTeam, ulong foolWinnerId, bool waitForNewspaper)
    {
        Debug.Log($"[GameManager] Game Over. Winner: {winningTeam}");
        IsGameOver.Value = true;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
            PlayerRole role = ServerRoleMap.ContainsKey(client.ClientId) ? ServerRoleMap[client.ClientId] : PlayerRole.Unassigned;
            bool isWinner = false;

            switch (winningTeam)
            {
                case WinTeam.Town:
                    if (role != PlayerRole.Impostor && role != PlayerRole.ImpostorControl && role != PlayerRole.Fool)
                        isWinner = true;
                    break;

                case WinTeam.Impostors:
                    if (role == PlayerRole.Impostor || role == PlayerRole.ImpostorControl)
                        isWinner = true;
                    break;

                case WinTeam.Fool:
                    if (client.ClientId == foolWinnerId)
                        isWinner = true;
                    break;
            }
            
            data.hasWon.Value = isWinner;
        }

        if (!waitForNewspaper)
        {
            Invoke(nameof(TriggerGameOver), 0.5f);
        }
        else
        {
            Debug.Log("[GameManager] Game is over, but waiting for Newspaper transition to finish before showing Game Over screen.");
        }
    }

    private void TriggerGameOver()
    {
        LobbyManager.Instance.UIChangeClientRpc();
    }
    
    private void Shuffle<T>(IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}