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

    //Otvori voting panel i stovri gumbe
    public void OpenVotingPanel()
    {
        //ClearButtons(); //Makni one stare gumbe da nema kopija

        //Projdi kroz sve spojene igrace i napravi gumb s njihovim imenom
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
            //Ako nije igrac mrtav napravi gumb za njega
            if (playerData.IsAlive.Value && !(buttonExists))
            {
                string playerName = playerData.PlayerName.Value.ToString();
                ulong targetClientId = client.ClientId;
                CreateVoteButton(playerName, targetClientId); //Napravim gumb koji ce na sebi imati tekst sa imenom igraaca, a biti ce zapamceno koji je id klijenta sa tim imenom
            }
        }

        //Skip gumb
        //CreateVoteButton("Skip", ulong.MaxValue);
    }

    //Zatvori voting panel ilitga makni gumbe
    public void CloseVotingPanel()
    {
        //ClearButtons();
    }

    //Funkcija za micanje gumbi kada se vote panel zatvara
    public void ClearButtons()
    {
        //Projdi kroz citavu listu i unisti gumbe sve
        foreach (Transform child in playerListParent)
        {
            Destroy(child.gameObject);
        }
    }

    //Funkcija za instaanciranje gumba
    private void CreateVoteButton(string name, ulong targetClientId)
    {
        //Instanciraj gumb i postavim tekst gumba nna poslano ime
        GameObject buttonObject = Instantiate(voteButtonPrefab, playerListParent);
        buttonObject.GetComponentInChildren<TMP_Text>().text = name;
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => {
            SelectVoteTarget(targetClientId, button);
        }); //Kada se klikne na taj gumb onda se glasa za igracem sa specificiranim ClinetId
        if (votes.TryGetValue(NetworkManager.Singleton.LocalClientId, out ulong votedFor) && votedFor == targetClientId)
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
        Debug.Log($"Selected vote target: {targetClientId}");
    }

    private void ClearVote()
    {
        if (selectedButton != null)
        {
            selectedButton.image.sprite = null;
            selectedButton.image.color = new Color(1f, 1f, 1f, 0f);
            selectedButton = null;
        }
        selectedTargetClientId = ulong.MaxValue;
    }

    //Funkcija za kada se klikne na gumb sa necijim imenom u vote panelu
    public void CastVote()
    {
        if (IsClient)
        {
            //Pošalji na server vote igraca
            CastVoteServerRpc(NetworkManager.Singleton.LocalClientId, selectedTargetClientId);
        }

        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} CONFIRMED vote for {selectedTargetClientId}");

        //Oznaci novi confirmed
        if (selectedButton != null){

            //Resetiraj prethodni confirmed gumb
            if (confirmedButton != null){
                confirmedButton.image.sprite = null;
                confirmedButton.image.color = new Color(1f, 1f, 1f, 0f);
            }

            //Postavi novi confirmed
            confirmedButton = selectedButton;
            confirmedButton.image.sprite = confirmCircle;
            confirmedButton.image.color = new Color(1f, 1f, 1f, 1f);

            selectedButton = null; //Resetiraj selekciju
        }
        //ClearVote();
    }

    //Za skip button
    public void CastSkipVote()
    {
        selectedTargetClientId = ulong.MaxValue;
        if (IsClient)
        {
            //Pošalji na server vote igraca
            CastVoteServerRpc(NetworkManager.Singleton.LocalClientId, selectedTargetClientId);
        }

        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} CONFIRMED vote for {selectedTargetClientId}");
        if (selectedButton != null && selectedButton != confirmedButton)
        {
            selectedButton.image.sprite = null;
            selectedButton.image.color = new Color(1f, 1f, 1f, 0f);
        }
        selectedButton = null;
        Button skipButton = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
        if (confirmedButton != null && confirmedButton != skipButton)
        {
            confirmedButton.image.sprite = null;
            confirmedButton.image.color = new Color(1f, 1f, 1f, 0f);
        }
        confirmedButton = skipButton;
        confirmedButton.image.sprite = confirmCircle;
        confirmedButton.image.color = new Color(1f, 1f, 1f, 1f);
        //CastVote();
    }

    //Prikupljanje glasova na serveru
    [ServerRpc(RequireOwnership = false)]
    private void CastVoteServerRpc(ulong voterClientId, ulong targetClientId)
    {
        /*
        //Ime igraca koje je bilo na gumbu
        string voteName;

        //Ako je id ulong.max onda se kliknulo na skip gumb i postavi ime igraca za kojeg se glasalo na Skip
        if (targetClientId == ulong.MaxValue) {
            voteName = "Skip";
        }

        //Inace dohvati preko klijnet id koje je ime tog igraca i pohrani
        else{
            voteName = GetPlayerNameByClientId(targetClientId);
        }
        */

        //Ako je ovaj igrac vec glasao za nesto promjeni njeogv odabir
        if (votes.ContainsKey(voterClientId))
        {
            votes[voterClientId] = targetClientId;
        }

        //Ako igrac nije glasao za nista napravi novi zapis
        else
        {
            votes.Add(voterClientId, targetClientId);
        }

        Debug.Log($"[Server] Received vote from {voterClientId} for {targetClientId}");
    }

    //Dobi ime igraca preko ClientId
    private string GetPlayerNameByClientId(ulong clientId)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == clientId)
            {
                var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
                return data.PlayerName.Value.ToString();
            }
        }
        return "Unknown";
    }

    //Dobi id clienta preko imena
    private ulong GetClientIdByPlayerName(string playerName)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
            if (data.PlayerName.Value.ToString() == playerName)
            {
                return client.ClientId;
            }
        }
        return ulong.MaxValue;
    }

    //Nakon sto istekne vrijeme
    public void ResolveVotes()
    {
        Dictionary<ulong, int> voteCounts = new Dictionary<ulong, int>();
        ulong defaultVote = ulong.MaxValue;

        //Za svakog klijenta pogledaj kako je on glasao, ako nema nista stavi da je glasao za "Skip"
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong clientId = client.ClientId;
            ulong vote;
            //Pogledaj za koga je glasao
            if (votes.ContainsKey(clientId))
            {
                vote = votes[clientId];
            }

            else
            {
                vote = defaultVote;
            }

            //Ukupni borj glasova za tu opciju
            if (voteCounts.ContainsKey(vote))
            {
                voteCounts[vote]++;
            }
            else
            {
                voteCounts[vote] = 1;
            }
        }

        //Tko je dobio najjvise glasova
        ulong result = defaultVote;
        int max = 0;
        foreach (var pair in voteCounts)
        {
            //Cista vecina
            if (pair.Value > max)
            {
                max = pair.Value;
                result = pair.Key;
            }

            //Nerjeseno
            else if (pair.Value == max)
            {
                result = ulong.MaxValue;
            }
        }

        //Stavi onaj dead bool onom koji je izbacen
        if (result != ulong.MaxValue)
        {
            //ulong targetClientId = GetClientIdByPlayerName(result);
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(result, out var client))
            {
                var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
                data.IsAlive.Value = false;
            }
        }

        //Dojavi rezultat
        VotingResultClientRpc(result);

        //Resetiranje
        votes.Clear();
        ClearVote();
        ClearButtons();
    }
    
    public bool[] CheckWinCondition()
    {
        int noAliveImpostors = 0;
        int noAliveGoodGuys = 0;
        int noPlayers = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkData>().noPlayers.Value;
        var role = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkData>().MyRole;
        bool is_impostor = (role == PlayerRole.Impostor || role == PlayerRole.ImpostorControl) ? true : false;
        bool[] results = new bool[2]; // [game_ended, has_won]

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var client_role = client.PlayerObject.GetComponent<PlayerNetworkData>().MyRole;
            var client_alive = client.PlayerObject.GetComponent<PlayerNetworkData>().IsAlive.Value;
            if ((client_role == PlayerRole.Impostor || client_role == PlayerRole.ImpostorControl) && client_alive)
                noAliveImpostors += 1;
            else if ((client_role != PlayerRole.Impostor && client_role != PlayerRole.ImpostorControl) && client_alive)
                noAliveGoodGuys += 1;
        }
        Debug.Log("Alive impostors: "+noAliveImpostors.ToString());
        Debug.Log("Alive good guys: " + noAliveGoodGuys.ToString());
        Debug.Log("Is impostor?: " + is_impostor.ToString());
        Debug.Log("Is alive?: " + NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkData>().IsAlive.Value);

        if(noAliveImpostors == 0)
        {
            results[0] = true;
            if (is_impostor)
                results[1] = false;
            else
                results[1] = true;
        }
        else
        {
            if(noAliveImpostors >= noAliveGoodGuys)
            {
                results[0] = true;
                if (is_impostor)
                    results[1] = true;
                else
                    results[1] = false;
            }
            else
            {
                results[0] = false;
                results[1] = false;
            }
        }
        return results;
    }


    //Dojaviti tko je bio izbacen za mjenjanje nekih UI elemenata ili sto ce biti potrebno
    [ClientRpc]
    private void VotingResultClientRpc(ulong result)
    {
        Debug.Log($"Voting result on client: {result}");
        string name = "Unknown";
        if (result == ulong.MaxValue) {
            name = "Skip";
        }
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(result, out var client))
        {
            var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
            name = data.PlayerName.Value.ToString();
        }
        Debug.Log($"Voted out player is: {name}");
        voteTexts.SetActive(true);
        voteTexts.gameObject.GetComponent<TMP_Text>().text = "Voting result: " + name;
    }
}