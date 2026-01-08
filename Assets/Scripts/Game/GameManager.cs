using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

// enum za role
public enum PlayerRole
{
    Unassigned,
    Villager,
    Investigator,
    Doctor,
    Impostor,
    ImpostorControl,
    Fool
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

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
        
        // Ids svih igraca
        List<ulong> playerIds = NetworkManager.Singleton.ConnectedClients.Keys.ToList();
        
        // random redoslijed
        Shuffle(playerIds);

        // Dicitonary za role
        Dictionary<ulong, PlayerRole> assignedRoles = new Dictionary<ulong, PlayerRole>();
        
        // prvi je impostor, mozemo napraviti tu da se usporeduje kolko je igraca pa vidit jel ak je 5-7 igraca 1 impostor, 8+ su 2 itd
        assignedRoles[playerIds[0]] = PlayerRole.Impostor;

        // Ostali su villageri i random role sa sansom
        for (int i = 1; i < playerIds.Count; i++)
        {
            float randomChance = Random.Range(0f, 1f);
            if (randomChance < 0.25f)
            {
                assignedRoles[playerIds[i]] = PlayerRole.Investigator;
            }
            else if (randomChance < 0.5f)
            {
                assignedRoles[playerIds[i]] = PlayerRole.Doctor;
            }
            else
            {
                assignedRoles[playerIds[i]] = PlayerRole.Villager;
            }
        }

        // Salje role playerima privatno
        foreach (var entry in assignedRoles)
        {
            ulong clientId = entry.Key;
            PlayerRole role = entry.Value;

            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            var playerData = playerObject.GetComponent<PlayerNetworkData>();
            
            // Ovo salje RPC specificnom klijentu
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            };

            playerData.SetRoleClientRpc(role, clientRpcParams);
            LobbyManager.Instance.RoleShowcaseParamsClientRpc(role, clientRpcParams);
        }
        
    }
    
    private void Shuffle<T>(IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}