using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

public class VoteingUI : NetworkBehaviour
{
    [Header("UI")]
    public Transform playerListParent; //Objekt koji drzi gumbe
    public GameObject voteButtonPrefab; //Prefab za gumb

    private Dictionary<ulong, string> votes = new Dictionary<ulong, string>(); //Zaa drzanje tko je za koga glasao clinetId -> ime igraca za kojeg glasa

    //Otvori voting panel i stovri gumbe
    public void OpenVotingPanel()
    {
        ClearButtons(); //Makni one stare gumbe da nema kopija

        //Projdi kroz sve spojene igrace i napravi gumb s njihovim imenom
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerData = client.PlayerObject.GetComponent<PlayerNetworkData>();

            //Ako nije igrac mrtav napravi gumb za njega
            if (!(playerData.dead.Value))
            {
                string playerName = playerData.PlayerName.Value.ToString();
                ulong targetClientId = client.ClientId;
                CreateVoteButton(playerName, targetClientId); //Napravim gumb koji ce na sebi imati tekst sa imenom igraaca, a biti ce zapamceno koji je id klijenta sa tim imenom
            }
        }

        //Skip gumb
        CreateVoteButton("Skip", ulong.MaxValue);
    }

    //Zatvori voting panel ilitga makni gumbe
    public void CloseVotingPanel()
    {
        ClearButtons();
    }

    //Funkcija za micanje gumbi kada se vote panel zatvara
    private void ClearButtons()
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
        button.onClick.AddListener(() => {CastVote(targetClientId);}); //Kada se klikne na taj gumb onda se glasa za igracem sa specificiranim ClinetId
    }

    //Funkcija za kada se klikne na gumb sa necijim imenom u vote panelu
    private void CastVote(ulong targetClientId)
    {
        if (IsClient)
        {
            //Pošalji na server vote igraca
            CastVoteServerRpc(NetworkManager.Singleton.LocalClientId, targetClientId);
        }

        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} voted for {targetClientId}");
    }

    //Prikupljanje glasova na serveru
    [ServerRpc(RequireOwnership = false)]
    private void CastVoteServerRpc(ulong voterClientId, ulong targetClientId)
    {
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

        //Ako je ovaj igrac vec glasao za nesto promjeni njeogv odabir
        if (votes.ContainsKey(voterClientId)){
            votes[voterClientId] = voteName;
        }

        //Ako igrac nije glasao za nista napravi novi zapis
        else{
            votes.Add(voterClientId, voteName);
        }

        Debug.Log($"[Server] Received vote from {voterClientId} for {voteName}");
    }

    //Dobi ime igraca preko ClientId
    private string GetPlayerNameByClientId(ulong clientId)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList){
            if (client.ClientId == clientId){
                var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
                return data.PlayerName.Value.ToString();
            }
        }
        return "Unknown";
    }

    //Dobi id clienta preko imena
    private ulong GetClientIdByPlayerName(string playerName) {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
            var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
            if (data.PlayerName.Value.ToString() == playerName) {
                return client.ClientId;
            }
        }
        return ulong.MaxValue;
    }

    //Nakon sto istekne vrijeme
    public void ResolveVotes()
    {
        Dictionary<string, int> voteCounts = new Dictionary<string, int>();
        string defaultVote = "Skip";

        //Za svakog klijenta pogledaj kako je on glasao, ako nema nista stavi da je glasao za "Skip"
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong clientId = client.ClientId;
            string vote;
            //Pogledaj za koga je glasao
            if (votes.ContainsKey(clientId)) {
                vote = votes[clientId];
            }

            else {
                vote = defaultVote;
            }

            //Ukupni borj glasova za tu opciju
            if (voteCounts.ContainsKey(vote))
            {
                voteCounts[vote]++;
            }
            else {
                voteCounts[vote] = 1;
            }
        }

        //Tko je dobio najjvise glasova
        string result = defaultVote;
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
                result = "Skip";
            }
        }

        //Stavi onaj dead bool onom koji je izbacen
        if (result != "Skip") {
            ulong targetClientId = GetClientIdByPlayerName(result);
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var client)) {
                var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
                data.dead.Value = true;
            }
        }

        //Dojavi rezultat
        VotingResultClientRpc(result);

        //Resetiranje
        votes.Clear();
    }

    //Dojaviti tko je bio izbacen za mjenjanje nekih UI elemenata ili sto ce biti potrebno
    [ClientRpc]
    private void VotingResultClientRpc(string result)
    { 
        Debug.Log($"Voting result on client: {result}");
    }
}