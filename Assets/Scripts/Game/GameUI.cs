using TMPro;
using Unity.Netcode;
using UnityEngine;

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
            dayTimeScreen.SetActive(false);
            if (votingScreen.activeInHierarchy)
            {
                votingScreen.SetActive(false);
                RectTransform rect = timer.GetComponent<RectTransform>();
                Vector2 pos = new Vector2(150f,-100f); //hardcoded = bad
                votingMenuButton.interactable = true;
                voteUI.CloseVotingPanel();
                rect.anchoredPosition = pos;
            }
            voteUI.ResolveVotes();
            ToggleTransition(0);
        }
        else if (nightTimeScreen.activeInHierarchy)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) lobby.ResetLocationClientRpc();
            // 2 cases: game continues = cycle to day, game ends = cycle to game over screen
            if (false)
            {
                //end the game
                nightTimeScreen.SetActive(false);
                if(NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    lobby.UIChangeClientRpc();
            }
            else
            {
                // continue the game
                nightTimeScreen.SetActive(false);
                ToggleTransition(1);
            }
        }
    }

    public void OpenVotingMenu()
    {
        RectTransform rect = timer.GetComponent<RectTransform>();
        Vector2 pos = rect.anchoredPosition;
        pos.y *= -1;
        rect.anchoredPosition = pos; 
        votingScreen.SetActive(true);
        voteUI.OpenVotingPanel();
        votingMenuButton.interactable = false;
    }
    public void CloseVotingMenu()
    {
        RectTransform rect = timer.GetComponent<RectTransform>();
        Vector2 pos = rect.anchoredPosition;
        pos.y *= -1;
        rect.anchoredPosition = pos;
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
}




#region Previous code
/*
    [SerializeField] private TMP_Text roleDisplayText;

    private void OnEnable()
    {
        if (NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            SubscribeToRoleEvent();
        }
        else
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            var localPlayerData = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkData>();
            if (localPlayerData != null)
            {
                localPlayerData.OnRoleAssigned -= DisplayRole;
            }
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SubscribeToRoleEvent();
        }
    }

    private void SubscribeToRoleEvent()
    {
        var localPlayerData = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkData>();
        localPlayerData.OnRoleAssigned += DisplayRole;

        if (localPlayerData.MyRole != PlayerRole.Unassigned)
        {
            DisplayRole(localPlayerData.MyRole);
        }
    }

    private void DisplayRole(PlayerRole role)
    {
        roleDisplayText.text = $"Your Role: {role}";
    }
    */
#endregion