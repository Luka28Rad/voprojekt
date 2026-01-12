using TMPro;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System;

public class GameUI : NetworkBehaviour 
{
    private static bool dayTimeTimerActive = false;
    private static bool nightTimeTimerActive = false;
    public static GameUI Instance { get; private set; }

    [SerializeField] private GameTimeManager gameTimeManager;
    [SerializeField] private LobbyManager lobby;
    [SerializeField] public TMP_Text timer;

    [Header("Day Time Screen")]
    [SerializeField] public GameObject dayTimeScreen;
    [SerializeField] private UnityEngine.UI.Button votingMenuButton;

    [Header("Night Time Screen")]
    [SerializeField] public GameObject nightTimeScreen;
    // add ability UI here

    [Header("Transition Time Screen")]
    [SerializeField] public GameObject transitionScreen;

    [Header("Voting Screen")]
    [SerializeField] public GameObject votingScreen;
    [SerializeField] public VoteingUI voteUI;
    [SerializeField] private UnityEngine.UI.Button voteButton;
    [SerializeField] private UnityEngine.UI.Button skipButton;
    [SerializeField] private UnityEngine.UI.Button exitButton;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if(Instance != this)
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        votingMenuButton.onClick.AddListener(OpenVotingMenu);
        voteButton.onClick.AddListener(ClientVote);
        skipButton.onClick.AddListener(ClientSkipVote);
        exitButton.onClick.AddListener(CloseVotingMenu);
        
        if (dayTimeScreen) dayTimeScreen.SetActive(false);
        if (nightTimeScreen) nightTimeScreen.SetActive(false);
        if (transitionScreen) transitionScreen.SetActive(false);
        if (votingScreen) votingScreen.SetActive(false);
        
        if (timer) timer.text = ""; 
    }
    private void Update()
    {
        if (dayTimeScreen.activeInHierarchy && dayTimeTimerActive)
        {
            float timeLeft = gameTimeManager.DayTime.Value;
            timer.text = Mathf.CeilToInt(timeLeft).ToString();
        }
        else if (nightTimeScreen.activeInHierarchy && nightTimeTimerActive)
        {
            float timeLeft = gameTimeManager.NightTime.Value;
            timer.text = Mathf.CeilToInt(timeLeft).ToString();
        }
    }
    public void EnableDayTimer()
    {
        dayTimeScreen.SetActive(true);
        dayTimeTimerActive = true;
    }
    public void DisableDayTimer()
    {
        dayTimeTimerActive = false;
    }
    public void EnableNightTimer()
    {
        nightTimeScreen.SetActive(true);
        nightTimeTimerActive = true;
    }
    public void DisableNightTimer()
    {
        nightTimeTimerActive = false;
    }
    private void ToggleTransition(int panelIndex)
    {
        gameTimeManager.StartTransitionTimer(panelIndex);
    }
    public void UIChange() 
    {
        if (!IsServer) return;

        if (dayTimeScreen.activeInHierarchy)
        {
            StartCoroutine(ResolveVotesFlow());
        }
        else if (nightTimeScreen.activeInHierarchy)
        {
            lobby.ResetLocationClientRpc();
            CleanupNightSeats();
            ToggleTransitionClientRpc(1);
        }
    }
    
    private void CleanupNightSeats()
    {
        for(int i = 1; i < 4; i++)
        {
            var cart_seat = nightTimeScreen.transform.GetChild(i);
            if (cart_seat.childCount == 1)
            {
                var playerCard = cart_seat.GetChild(0);
                var playerCardsContainer = lobby.GetPlayerCardsContainer();
                playerCard.SetParent(playerCardsContainer, false);
            }
        }
        nightTimeScreen.SetActive(false);
    }

    public void OpenVotingMenu()
    {
        votingScreen.SetActive(true);
        voteUI.OpenVotingPanel();
        votingMenuButton.interactable = false;
    }
    public void CloseVotingMenu()
    {
        votingScreen.SetActive(false);
        voteUI.CloseVotingPanel();
        votingMenuButton.interactable = true;
    }
    public void ClientVote()
    {

    }
    public void ClientSkipVote()
    {

    }
    
    public void ServerEndDay()
    {
        Debug.Log("Server: Day Ended. Starting Vote Resolution.");
        StartCoroutine(ResolveVotesFlow());
    }

    private IEnumerator ResolveVotesFlow()
    {
        voteUI.ResolveVotes();
        yield return new WaitForSeconds(gameTimeManager.voteResultTime);
        CloseVotingScreenClientRpc();
        ToggleTransitionClientRpc(0);
    }
    
    [ClientRpc]
    private void ToggleTransitionClientRpc(int panelIndex)
    {
        if(nightTimeScreen.activeInHierarchy) nightTimeScreen.SetActive(false);
        gameTimeManager.StartTransitionTimer(panelIndex);
    }
    
    [ClientRpc]
    private void CloseVotingScreenClientRpc()
    {
        voteUI.voteTexts.SetActive(false);
        dayTimeScreen.SetActive(false);
        if (votingScreen.activeInHierarchy)
        {
            votingScreen.SetActive(false);
            RectTransform rect = timer.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(150f, -100f);
            votingMenuButton.interactable = true;
            voteUI.ClearButtons();
        }
    }

}