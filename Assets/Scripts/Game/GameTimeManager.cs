using NUnit.Framework.Constraints;
using System.Collections;
using System.Threading;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

public class GameTimeManager : NetworkBehaviour
{
    [SerializeField] LobbyManager lobby;
    [SerializeField] GameUI gameUI;
    [SerializeField] Sprite cart1;
    [SerializeField] Sprite cart2;
    [SerializeField] Sprite cart3;
    [SerializeField] Sprite armrest1;
    [SerializeField] Sprite armrest2;
    [SerializeField] Sprite armrest3;
    public NetworkVariable<float> ShowcaseTime = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
    );
    public NetworkVariable<float> DayTime = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
    );
    public NetworkVariable<float> NightTime = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
    );
    public NetworkVariable<float> GameOverTime = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
    );
    [SerializeField] private float roleShowcaseDuration;
    [SerializeField] private float dayTimeDuration;
    [SerializeField] private float nightTimeDuration;
    [SerializeField] private float gameOverTimeDuration;
    [SerializeField] private float transitionDuration;
    [SerializeField] private float newspaperDuration;
    public Coroutine transitionRoutine;

    #region helperRPCs
    [ClientRpc] private void UIChangeClientRpc()
    {
        GameUI.Instance.UIChange();
    }
    [ClientRpc] private void EnableDayTimeTimerClientRpc()
    {
        GameUI.Instance.EnableDayTimer();
    }
    [ClientRpc] private void DisableDayTimeTimerClientRpc()
    {
        GameUI.Instance.DisableDayTimer();
    }
    [ClientRpc]
    private void EnableNightTimeTimerClientRpc()
    {
        GameUI.Instance.EnableNightTimer();
    }
    [ClientRpc]
    private void DisableNightTimeTimerClientRpc()
    {
        GameUI.Instance.DisableNightTimer();
    }
    #endregion

    public void StartShowcaseTimer()
    {
        if (IsServer)
        {
            ShowcaseTime.Value = roleShowcaseDuration;
            Debug.Log("SHOWCASE TIMER STARTED");
            StartCoroutine(ShowcaseCountdown());
        }
    }
    private IEnumerator ShowcaseCountdown()
    {
        while (ShowcaseTime.Value > 0)
        {
            ShowcaseTime.Value -= Time.deltaTime;
            yield return null;
        }
        ShowcaseTime.Value = 0;
        lobby.UIChangeClientRpc();
    }
    public void StartDayTimeTimer()
    {
        if (IsServer)
        {
            DayTime.Value = dayTimeDuration;
            Debug.Log("DAYTIME TIMER STARTED");
            EnableDayTimeTimerClientRpc();
            StartCoroutine(DayTimeCountdown());
        }
    }
    private IEnumerator DayTimeCountdown()
    {
        while (DayTime.Value > 0)
        {
            DayTime.Value -= Time.deltaTime;
            yield return null;
        }
        DayTime.Value = 0;
        DisableDayTimeTimerClientRpc();
        UIChangeClientRpc();
    }
    public void StartNightTimeTimer()
    {
        if (IsServer)
        {
            NightTime.Value = nightTimeDuration;
            Debug.Log("DAYTIME TIMER STARTED");
            EnableNightTimeTimerClientRpc();
            StartCoroutine(NightTimeCountdown());
        }
    }
    private IEnumerator NightTimeCountdown()
    {
        while (NightTime.Value > 0)
        {
            NightTime.Value -= Time.deltaTime;
            yield return null;
        }
        NightTime.Value = 0;
        
        if (ActionResolutionSystem.Instance != null)
        {
            Debug.Log("Night ended. Resolving actions...");
            ActionResolutionSystem.Instance.ResolveTurn();
        }
        
        DisableNightTimeTimerClientRpc();
        UIChangeClientRpc();
    }
    public void StartWaitingRoomTimer() //uses gameover timer
    {
        if (IsServer)
        {
            GameOverTime.Value = gameOverTimeDuration;
            Debug.Log("GAME OVER TIMER STARTED");
            StartCoroutine(WaitingRoomCountdown());
        }
    }
    private IEnumerator WaitingRoomCountdown()
    {
        while (GameOverTime.Value > 0)
        {
            GameOverTime.Value -= Time.deltaTime;
            yield return null;
        }
        GameOverTime.Value = 0;
        lobby.UIChangeClientRpc();
    }
    public void StartTransitionTimer(int panelIndex)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionCountdown(panelIndex));
    }
    
    private bool CheckIfAnyoneDied()
    {
        if (ActionResolutionSystem.Instance != null)
        {
            return ActionResolutionSystem.Instance.LastNightVictims.Count > 0;
        }
        return false;
    }
    
    private IEnumerator TransitionCountdown(int panelIndex)
    {
        gameUI.timer.gameObject.SetActive(false);
        gameUI.transitionScreen.SetActive(true);
        for (int i = 0; i < gameUI.transitionScreen.transform.childCount; i++)
            gameUI.transitionScreen.transform.GetChild(i).gameObject.SetActive(false);
        if (panelIndex == 1)
        {
            Transform newspaperScreen = gameUI.transitionScreen.transform.GetChild(2);
            newspaperScreen.gameObject.SetActive(true);
            NewspaperAnimation news = null;
            int child_index = -1;
            bool someoneDied = CheckIfAnyoneDied(); 
            Debug.Log("Someone died: " + someoneDied);
            if (!someoneDied) 
                child_index = 0;
            else 
            {
                child_index = 1;
            
                LobbyManager lobbyManager = FindObjectOfType<LobbyManager>(); 
                GameObject playerCardsContainer = lobbyManager.playerContainer; 
                FindAndReparentDeadPlayer(playerCardsContainer.transform);
            }
            newspaperScreen.GetChild(child_index).gameObject.SetActive(true);
            news = newspaperScreen.GetChild(child_index).gameObject.GetComponent<NewspaperAnimation>();
            news.PlayAnimation();
            yield return new WaitForSeconds(newspaperDuration-news.duration);
            newspaperScreen.GetChild(child_index).gameObject.SetActive(false);
            if (child_index == 1)
            {
                var dead_player = newspaperScreen.GetChild(1).transform.GetChild(0).transform.GetChild(0);
                Debug.Log(dead_player.name+dead_player.transform.childCount.ToString());
                int index = 0;
                Color32 color = new Color32(0, 0, 0, 255); //indicator color for a dead player
                for (int i = 1; i <= 7 && i < dead_player.childCount; i++)
                {
                    Debug.Log("Child component " + i.ToString() + dead_player.GetChild(i).name);
                    var img = dead_player.GetChild(i).GetComponent<Image>();
                    if (img != null) img.color = color;
                }
                dead_player.GetChild(0).GetComponent<TMP_Text>().text = "<s>"+dead_player.GetChild(0).GetComponent<TMP_Text>().text+"</s>";
                dead_player.SetParent(lobby.GetPlayerCardsContainer(),false);
            }
            newspaperScreen.gameObject.SetActive(false);
        }
        var panel = gameUI.transitionScreen.transform.GetChild(panelIndex);
        panel.gameObject.SetActive(true);
        var train = panel.GetChild(1).GetComponent<TrainAnimation>();
        train.PlayAnimation();
        yield return new WaitForSeconds(transitionDuration-train.duration);
        panel.gameObject.SetActive(false);
        gameUI.transitionScreen.SetActive(false);
        gameUI.timer.gameObject.SetActive(true);
        if (panelIndex == 0) // Ovo je night time
        {
            var localClient = NetworkManager.Singleton.LocalClient;
            var localData = localClient.PlayerObject.GetComponent<PlayerNetworkData>();
            int myCart = localData.TrainCart.Value;

            var bg_image = gameUI.nightTimeScreen.GetComponent<Image>();
            var armrest_image = gameUI.nightTimeScreen.transform.GetChild(0).GetComponent<Image>();

            if (myCart == 1) { bg_image.sprite = cart1; armrest_image.sprite = armrest1; }
            else if (myCart == 2) { bg_image.sprite = cart2; armrest_image.sprite = armrest2; }
            else if (myCart == 3) { bg_image.sprite = cart3; armrest_image.sprite = armrest3; }
            gameUI.nightTimeScreen.SetActive(true);
            
            Transform[] seatSlots = new Transform[3];
            seatSlots[0] = gameUI.nightTimeScreen.transform.GetChild(1);
            seatSlots[1] = gameUI.nightTimeScreen.transform.GetChild(2);
            seatSlots[2] = gameUI.nightTimeScreen.transform.GetChild(3);
            
            EditPlayerLook[] allVisualCards = FindObjectsByType<EditPlayerLook>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Transform hiddenContainer = lobby.GetPlayerCardsContainer(); 
            int currentSlotIndex = 0;
            
            foreach (var card in allVisualCards)
            {
                // Find the NetworkData associated with this card
                PlayerNetworkData cardOwnerData = null;
            
                // Match the card's ClientID to a PlayerObject
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(card.linkedClientId, out NetworkClient client))
                {
                    cardOwnerData = client.PlayerObject.GetComponent<PlayerNetworkData>();
                }

                if (cardOwnerData != null)
                {
                    bool isMe = card.linkedClientId == localClient.ClientId;
                    bool isSameCart = cardOwnerData.TrainCart.Value == myCart;
                    bool isAlive = cardOwnerData.IsAlive.Value;

                    // If it is NOT me, IS in my cart, and IS alive -> Put in seat
                    if (!isMe && isSameCart && isAlive && currentSlotIndex < 3)
                    {
                        card.transform.SetParent(seatSlots[currentSlotIndex], false);
                        card.transform.localPosition = Vector3.zero;
                        card.transform.localScale = Vector3.one; // Ensure scale is correct
                        card.enabled = true;
                        // Enable interaction button on this card (if you have one)
                        // card.EnableInteractionButton(true); 
                    
                        currentSlotIndex++;
                    }
                    else if (!isMe)
                    {
                        // Hide players not in my cart / dead players / or if slots are full
                        // Move them to the generic lobby container so they aren't visible
                        card.transform.SetParent(hiddenContainer, false);
                    }
                    // If isMe == true, we usually don't show our own card on screen, 
                    // or we leave it in the default container.
                }
            }
            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                StartNightTimeTimer();
        }
        else if (panelIndex == 1)
        { 
            gameUI.dayTimeScreen.SetActive(true);
            
            EditPlayerLook[] allVisualCards = FindObjectsOfType<EditPlayerLook>();
            Transform mainContainer = lobby.GetPlayerCardsContainer();
            foreach(var card in allVisualCards)
            {
                card.transform.SetParent(mainContainer, false);
            }

            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                StartDayTimeTimer();
        }
        transitionRoutine = null;
    }

    //Ovo za sada dohvati prvog u listi kojeg najde da ima dead = true, kada dodamo onaj kao stog ili sto vec sa svim mrtvima treba promjeniti da
    //uzme prvog od tih kojeg najde
    private void FindAndReparentDeadPlayer(Transform parent)
    {
        if (ActionResolutionSystem.Instance == null) return;

        foreach (ulong deadId in ActionResolutionSystem.Instance.LastNightVictims)
        {
            EditPlayerLook[] allCards = FindObjectsByType<EditPlayerLook>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var card in allCards)
            {
                if (card.linkedClientId == deadId)
                {
                    foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                    {
                        var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
                        if (card.linkedClientId == client.ClientId && !data.IsAlive.Value)
                        {
                            Transform deadParent = gameUI.transitionScreen.transform;

                            for (int i = 0; i < 3; i++)
                            {
                                deadParent = deadParent.GetChild(deadParent.childCount - 1);
                            }

                            card.transform.SetParent(deadParent, false);
                            card.transform.localPosition = Vector3.zero;
                            card.transform.SetAsLastSibling();
                            return;
                        }
                    }
                }
            }
        }
    }
}
