using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class VoteingUI : NetworkBehaviour
{
    [Header("UI")]
    public Transform playerListParent; //Objekt koji drzi gumbe
    public GameObject voteButtonPrefab; //Prefab za gumb
    public GameObject voteTexts;
    public Sprite selectCircle;
    public Sprite confirmCircle;

    [SerializeField] LobbyManager lobby;
    private Dictionary<ulong, ulong> votes = new Dictionary<ulong, ulong>(); //Zaa drzanje tko je za koga glasao clinetId -> clinetID igraca za kojeg glasa
    private ulong selectedTargetClientId = ulong.MaxValue;
    private Button selectedButton = null;
    private Button confirmedButton = null;

    public void OpenVotingPanel()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerData = client.PlayerObject.GetComponent<PlayerNetworkData>();
            bool buttonExists = false;
            foreach (Transform child in playerListParent)
            {
                TMP_Text text = child.GetComponentInChildren<TMP_Text>();
                if (text != null && text.text == playerData.PlayerName.Value.ToString())
                {
                    buttonExists = true;
                    break;
                }
            }
            if (playerData.IsAlive.Value && !buttonExists)
            {
                CreateVoteButton(playerData.PlayerName.Value.ToString(), client.ClientId);
            }
        }
    }

    public void CloseVotingPanel()
    {
        //ClearButtons();
    }

    public void ClearButtons()
    {
        foreach (Transform child in playerListParent) Destroy(child.gameObject);
    }

    private void CreateVoteButton(string name, ulong targetClientId)
    {
        GameObject buttonObject = Instantiate(voteButtonPrefab, playerListParent);
        buttonObject.GetComponentInChildren<TMP_Text>().text = name;
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => {
            SelectVoteTarget(targetClientId, button);
        });
        
        if (votes.ContainsKey(NetworkManager.Singleton.LocalClientId) && votes[NetworkManager.Singleton.LocalClientId] == targetClientId)
        {
            button.image.sprite = confirmCircle;
            confirmedButton = button;
        }
    }

    private void SelectVoteTarget(ulong targetClientId, Button button)
    {
        if (selectedButton != null && selectedButton != confirmedButton)
        {
            selectedButton.image.sprite = null;
            selectedButton.image.color = new Color(1f, 1f, 1f, 0f);
        }
        selectedTargetClientId = targetClientId;
        selectedButton = button;
        if (selectedButton != confirmedButton)
        {
            button.image.sprite = selectCircle;
            button.image.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    public void CastVote()
    {
        if (IsClient) CastVoteServerRpc(NetworkManager.Singleton.LocalClientId, selectedTargetClientId);

        if (selectedButton != null)
        {
            if (confirmedButton != null)
            {
                confirmedButton.image.sprite = null;
                confirmedButton.image.color = new Color(1f, 1f, 1f, 0f);
            }
            confirmedButton = selectedButton;
            confirmedButton.image.sprite = confirmCircle;
            confirmedButton.image.color = new Color(1f, 1f, 1f, 1f);
            selectedButton = null;
        }
    }

    public void CastSkipVote()
    {
        selectedTargetClientId = ulong.MaxValue;
        if (IsClient) CastVoteServerRpc(NetworkManager.Singleton.LocalClientId, selectedTargetClientId);

        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} CONFIRMED vote for {selectedTargetClientId}");
        if (selectedButton != null && selectedButton != confirmedButton)
        {
            selectedButton.image.sprite = null;
            selectedButton.image.color = new Color(1f, 1f, 1f, 0f);
        }
        selectedButton = null;
        // Logic for skip visual button...
    }

    [ServerRpc(RequireOwnership = false)]
    private void CastVoteServerRpc(ulong voterClientId, ulong targetClientId)
    {
        if (votes.ContainsKey(voterClientId)) votes[voterClientId] = targetClientId;
        else votes.Add(voterClientId, targetClientId);
    }
    
    public void ResolveVotes()
    {
        if (!IsServer) return;

        Dictionary<ulong, int> voteCounts = new Dictionary<ulong, int>();
        ulong defaultVote = ulong.MaxValue;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong vote = votes.ContainsKey(client.ClientId) ? votes[client.ClientId] : defaultVote;
            if (voteCounts.ContainsKey(vote)) voteCounts[vote]++;
            else voteCounts[vote] = 1;
        }

        ulong result = defaultVote;
        int max = 0;
        foreach (var pair in voteCounts)
        {
            if (pair.Value > max)
            {
                max = pair.Value;
                result = pair.Key;
            }
            else if (pair.Value == max)
            {
                result = ulong.MaxValue;
            }
        }

        VotingResultClientRpc(result);

        if (result != ulong.MaxValue)
        {
            GameManager.Instance.ExilePlayer(result);
        }
        else
        {
            GameManager.Instance.CheckWinCondition();
        }

        votes.Clear();
    }
    
    [ClientRpc]
    private void VotingResultClientRpc(ulong result)
    {
        string name = "Skip";
        if (result != ulong.MaxValue && NetworkManager.Singleton.ConnectedClients.TryGetValue(result, out var client))
        {
            var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
            name = data.PlayerName.Value.ToString();
        }
        voteTexts.SetActive(true);
        voteTexts.GetComponent<TMP_Text>().text = "Voting result: " + name;
        
        ClearButtons();
        selectedButton = null;
        confirmedButton = null;
        selectedTargetClientId = ulong.MaxValue;
    }
}