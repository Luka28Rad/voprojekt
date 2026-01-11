using TMPro;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class GameUI : MonoBehaviour
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
    private void OnEnable()
    {
        Debug.Log("IM GETTING CALLED WHEN ACTIVATING");
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            gameTimeManager.StartDayTimeTimer();
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
    public void UIChange() // make all clients proceed to the correct UI
    {
        if (dayTimeScreen.activeInHierarchy)
        {
            StartCoroutine(ResolveVotesFlow());
        }
        else if (nightTimeScreen.activeInHierarchy)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) lobby.ResetLocationClientRpc();
            bool[] stats = voteUI.CheckWinCondition();
            // 2 cases: game continues = cycle to day, game ends = cycle to game over screen
            if (stats[0])
            {
                //end the game
                NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkData>().hasWon.Value = true;
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    lobby.UIChangeClientRpc();
            }
            else
            {
                // continue the game
                for(int i = 1; i < 4; i++)
                {
                    var cart_seat = nightTimeScreen.transform.GetChild(i);
                    if (cart_seat.childCount == 1)
                    {
                        var playerCard = cart_seat.GetChild(0);
                        var playerCardsContainer = lobby.GetPlayerCardsContainer();
                        playerCard.SetParent(playerCardsContainer,false);
                    }
                }
                nightTimeScreen.SetActive(false);
                ToggleTransition(1);
            }
        }
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


    private IEnumerator ResolveVotesFlow()
    {
        voteUI.ResolveVotes();

        yield return new WaitForSeconds(gameTimeManager.voteResultTime);
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
        
        bool[] stats = voteUI.CheckWinCondition();
        if (stats[0])
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkData>().hasWon.Value = true;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                lobby.UIChangeClientRpc();
        }
        else
            ToggleTransition(0);
    }

}