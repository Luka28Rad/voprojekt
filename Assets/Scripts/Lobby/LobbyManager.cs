using System.Net;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph;
using System.Collections.Generic;

// Nasljeđuje NetworkBehaviour, što je osnova za sve skripte koje trebaju mrežnu funkcionalnost u Netcode for GameObjects (NGO).
// To nam daje pristup svojstvima poput IsHost, IsServer, IsClient i mrežnim metodama.
public class LobbyManager : NetworkBehaviour
{
    // lazy approach to roles descriptions
    public Dictionary<PlayerRole, string> desc = new Dictionary<PlayerRole, string>();

    private static int MIN_PLAYER_TO_START_GAME = 2;
    public static LobbyManager Instance { get; private set; }
    public static string PlayerName { get; private set; }

    private Transform playerCardsContainer;
    private GameObject playerModel;

    [SerializeField] private GameTimeManager Timer;
    [SerializeField] private PlayerUIManager PlayerUI;
    [SerializeField] private GameUI GameUI;

    [Header("Screens")]
    [SerializeField] private GameObject mainMenuScreen;
    [SerializeField] private GameObject waitingRoomScreen;
    [SerializeField] private GameObject lobbyScreen;
    [SerializeField] private GameObject roleShowcaseScreen;
    [SerializeField] private GameObject gameScreen;
    [SerializeField] private GameObject gameOverScreen;

    [Header("Main Menu Screen")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private UnityEngine.UI.Button hostButton;
    [SerializeField] private UnityEngine.UI.Button connectButton;

    [Header("Waiting Room Screen")]
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private TMP_Text numberOfConnectedPlayers;
    [SerializeField] private UnityEngine.UI.Button joinButton;
    [SerializeField] private UnityEngine.UI.Button startLobbyButton;
    [SerializeField] private UnityEngine.UI.Button exitButton;
    [SerializeField] private TMP_Text joinCodeText;

    [Header("Lobby Screen")]
    [SerializeField] private UnityEngine.UI.Button startGameButton;
    [SerializeField] private GameObject playerCardPrefab;
    

    [Header("Role Showcase Screen")]
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text roleDescription;
    //maybe add a direct reference to the sprite renderer as well

    [Header("Game Screen")]
    [SerializeField] private GameObject playerContainer;
    [SerializeField] private Transform cart1;
    [SerializeField] private Transform cart2;

    [Header("Game Over Screen")]
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private UnityEngine.UI.Button exitToMainMenuButton;
    [SerializeField] private TMP_Text nextMatchTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            desc.Add(PlayerRole.Unassigned, "???");
            desc.Add(PlayerRole.Villager, "Your average 9-5 worker trying to do his best in life. Help find killers and vote them out.");
            desc.Add(PlayerRole.Doctor, "Your part of the elite. Help the poor by shielding them for one night from any attackers. You can't shield a person consecutively.");
            desc.Add(PlayerRole.Investigator, "You have a new case, Columbo. You have to interogate people to find out on which side they are on.");
            desc.Add(PlayerRole.Impostor, "Its time to hunt! Kill all humans to get your sweet victory.");
        }
    }

    private void Start()
    {
        hostButton.onClick.AddListener(HostLobby);
        connectButton.onClick.AddListener(ShowJoinScreen);
        joinButton.onClick.AddListener(ConnectToLobby);
        startGameButton.onClick.AddListener(StartGame);
        startLobbyButton.onClick.AddListener(StartLobby);
        exitButton.onClick.AddListener(ExitToMainMenu);
        exitToMainMenuButton.onClick.AddListener(ExitToMainMenu);
        
        nameInputField.onValueChanged.AddListener(OnNameChanged);

        hostButton.interactable = false;
        connectButton.interactable = false;

        mainMenuScreen.SetActive(true);
        waitingRoomScreen.SetActive(false);
        lobbyScreen.SetActive(false);
        gameScreen.SetActive(false);

        playerCardsContainer = PlayerUI.GetContainer();
    }
    
    [ClientRpc] public void UIChangeClientRpc() // make all clients proceed to the correct UI
    {
        if (waitingRoomScreen.activeInHierarchy)
        {
            Debug.Log("Clients: changing from waiting room to lobby");
            waitingRoomScreen.SetActive(false);
            lobbyScreen.SetActive(true);
            playerCardsContainer.gameObject.SetActive(true); //Make player cards appear
        }
        else if (lobbyScreen.activeInHierarchy)
        {
            Debug.Log("Clients: changing from lobby to role showcase");
            lobbyScreen.SetActive(false);
            playerModel.GetComponent<EditPlayerLook>().DisableButtons();
            playerCardsContainer.gameObject.SetActive(false);
            roleShowcaseScreen.SetActive(true);
        }
        else if (roleShowcaseScreen.activeInHierarchy)
        {
            Debug.Log("Clients: changing from role showcase to game screen(day)");
            roleShowcaseScreen.SetActive(false);
            //move the cards container for each player down
            var rt = playerCardsContainer.GetComponent<RectTransform>();
            rt.SetParent(gameScreen.transform, false);
            rt.SetSiblingIndex(1);
            rt.anchoredPosition = new Vector2(5f, -65f);
            playerCardsContainer.SetSiblingIndex(1);
            var grid = playerCardsContainer.GetComponent<GridLayoutGroup>();
            //grid.cellSize = new Vector2(140, 180);
            //grid.spacing = new Vector2(15, 15);
            playerModel.transform.SetParent(playerContainer.transform, false);
            /*
             * foreach (Transform child in playerCardsContainer)
               {
                 //child.GetComponent<RectTransform>().localScale = Vector3.one;
                 //child.GetComponent<RectTransform>().sizeDelta *= 0.5f;
               }
            */
            playerCardsContainer.gameObject.SetActive(true);
            gameScreen.SetActive(true);
        }
        else if (gameScreen.activeInHierarchy)
        {
            Debug.Log("Clients: changing from game screen to game over screen");
            gameScreen.SetActive(false);
            gameOverScreen.SetActive(true);
            if (IsHost)
            {
                Timer.StartWaitingRoomTimer();
                Timer.ShowcaseTime.Value = 0;
                Timer.DayTime.Value = 0;
                Timer.NightTime.Value = 0;
            }
        }
        else if (gameOverScreen.activeInHierarchy)
        {
            Debug.Log("Clients: changing from game over screen back to waiting room");
            gameOverScreen.SetActive(false);
            waitingRoomScreen.SetActive(true);
        }
    }
    private void Update()
    {
        if (waitingRoomScreen.activeInHierarchy && IsHost){
            numberOfConnectedPlayers.text = NetworkManager.Singleton.ConnectedClientsList.Count().ToString();
        }
        if (gameOverScreen.activeInHierarchy)
        {
            float timeLeft = Timer.GameOverTime.Value;
            nextMatchTimer.text = Mathf.CeilToInt(timeLeft).ToString();
        }
        if (gameScreen.activeInHierarchy && GameUI.dayTimeScreen.activeInHierarchy)
        {
            foreach (Transform child in playerCardsContainer)
            {
                var edit = child.gameObject.GetComponent<EditPlayerLook>();
                int cart = edit.networkData.TrainCart.Value;
                if (cart != 0)
                {
                    if (cart == 1)
                    {
                        child.SetParent(cart1, false);
                    }
                    else if(cart == 2)
                    {
                        child.SetParent(cart2, false);
                    }
                }
                else if(cart==0 && edit.linkedClientId != NetworkManager.Singleton.LocalClientId)
                {
                    child.SetParent(playerCardsContainer, false);
                }
            }
        }
    }
    // this method gets used to set role descriptions and the color of the role name [green = good, red = bad]
    [ClientRpc] public void RoleShowcaseParamsClientRpc(PlayerRole roleName, ClientRpcParams clientRpcParams = default)
    {
        roleText.text = roleName.ToString();
        if (roleName == PlayerRole.Impostor)
            roleShowcaseScreen.transform.GetChild(1).GetComponent<TMP_Text>().color = new Color(200, 0, 0);
        else
            roleShowcaseScreen.transform.GetChild(1).GetComponent<TMP_Text>().color = new Color(0, 200, 0);
        roleDescription.text = desc[roleName];
    }
    [ClientRpc] public void ResetLocationClientRpc()
    {
        foreach (Transform child in cart1)
        {
            child.SetParent(playerCardsContainer, false);
        }
        foreach (Transform child in cart2)
        {
            child.SetParent(playerCardsContainer, false);
        }
        playerModel.transform.SetParent(playerContainer.transform, false);
        var data = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkData>();
        data.SetLocationServerRpc(0);
    }
    private void StartGame() // Lobby --> ShowCaseScreen
    {
        // IsHost je svojstvo iz NetworkBehaviour-a koje je istinito samo na računalu koje je pokrenulo igru kao host.
        // Osiguravamo da samo host može započeti igru, što je standardna praksa za autoritativni model.
        if (!IsHost) return;
        GameManager.Instance.StartGameAndAssignRoles();
        UIChangeClientRpc();
        Timer.StartShowcaseTimer();
    }

    #region MainMenuLogic
    // OnNetworkSpawn se poziva kada je ovaj NetworkObject stvoren na mreži (i za hosta i za klijente).
    // Ovo je pravo mjesto za prijavu na mrežne događaje jer NetworkManager sigurno postoji u ovom trenutku.
    public override void OnNetworkSpawn()
    {
        // Prijavljujemo se na callbackove (događaje) NetworkManager-a.
        // Ovi događaji će se aktivirati na svim klijentima kada se netko spoji ili odspoji.
        if (IsClient)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }

    // OnNetworkDespawn se poziva prije uništavanja NetworkObject-a.
    // Ključno je odjaviti se s događaja kako bi se izbjegli "memory leaks" i greške.
    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void OnNameChanged(string newName)
    {
        bool isNameValid = !string.IsNullOrWhiteSpace(newName);
        hostButton.interactable = isNameValid;
        connectButton.interactable = isNameValid;
    }

    public void HostLobby()
    {
        PlayerName = nameInputField.text;

        // Postavljamo podatke za Unity Transport sloj. Ovdje definiramo IP adresu i port
        // na kojem će server "slušati" dolazeće konekcije. "0.0.0.0" znači "sve dostupne mreže".
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            "0.0.0.0",
            7777
        );

        // StartHost() pokreće i server i klijenta na istom računalu.
        // Osoba koja ovo pokrene je i server i igrač (client).
        NetworkManager.Singleton.StartHost();
        for (int i = 0; i < 4; i++)
        {
            waitingRoomScreen.transform.GetChild(i).gameObject.SetActive(true);
        }
        joinCodeText.text = $"IP: {GetLocalIPv4()}";
        StartWaitingRoom();

    }

    public void ShowJoinScreen()
    {
        Debug.Log("Joined waiting room.");
        PlayerName = nameInputField.text;
        for (int i = 4; i < 7; i++)
        {
            waitingRoomScreen.transform.GetChild(i).gameObject.SetActive(true);
        }
        StartWaitingRoom();
    }
    #endregion

    // Ovo je callback funkcija koja se izvršava na SVIM spojenim klijentima (uključujući hosta)
    // svaki put kada se novi klijent uspješno spoji na server.
    private void HandleClientConnected(ulong clientId)
    {
        // 'clientId' je jedinstveni identifikator koji mreža dodjeljuje svakom spojenom klijentu.

        // IsServer provjerava da li se ovaj kod izvršava na serveru/hostu.
        // Samo server treba logirati tko se spojio i ažurirati UI za sve.
        if (IsServer)
        {
            Debug.Log($"Klijent {clientId} se spojio.");
            UpdateLobbyUI();
        }

        // Svaki klijent provjerava da li je on taj koji se upravo spojio.
        // NetworkManager.Singleton.LocalClientId je ID ovog lokalnog klijenta.
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Uspješno spojen na hosta.");
            joinCodeText.text = "Spojen na Host";
        }
    }
    
    // Slično kao i HandleClientConnected, ova funkcija se poziva na svim preostalim
    // klijentima kada se neki klijent odspoji.

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"Klijent {clientId} se odspojio.");
        // Ažuriramo UI da uklonimo igrača koji je otišao. To radi samo server.
        if(IsServer)
        {
            UpdateLobbyUI();
        }

    }
    public void UpdateLobbyUI()
    {
        foreach (Transform child in playerCardsContainer)
        {
            Destroy(child.gameObject);
        }

        // Prolazimo kroz ConnectedClientsList, listu svih trenutno spojenih klijenata
        // koju održava NetworkManager na serveru.
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {

            // Svaki klijent na mreži ima svoj PlayerObject, koji je NetworkObject koji ga predstavlja u igri.
            GameObject card = Instantiate(playerCardPrefab, playerCardsContainer);
            var playerNetworkData = client.PlayerObject.GetComponent<PlayerNetworkData>();

            //spremimo referencu player modela koji trenutni igrač koristi
            if (client.ClientId == NetworkManager.LocalClientId)
            {
                playerModel = card;
            }
                
            
            // Poveži svaku istancu kartice sa odgovarajucim klijentom
            var editLook = card.GetComponent<EditPlayerLook>();
            editLook.networkData = playerNetworkData;
            editLook.LinkClientId(client.ClientId);

            // Pristupamo NetworkVariable<T> varijabli. ".Value" se koristi za čitanje sinkronizirane vrijednosti.
            // U ovom slučaju, čitamo ime igrača koje je sinkronizirano preko mreže.
            card.GetComponent<PlayerCard>().SetPlayerName(playerNetworkData.PlayerName.Value.ToString());
        }

        if (IsHost)
        {
            // NetworkManager.Singleton.ConnectedClientsList.Count daje nam trenutni broj igrača.
            bool canStart = NetworkManager.Singleton.ConnectedClientsList.Count >= MIN_PLAYER_TO_START_GAME;
            startGameButton.gameObject.SetActive(canStart);
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
        }
    }

    // Ovo getta ip za join
    private string GetLocalIPv4()
    {
        return Dns.GetHostEntry(Dns.GetHostName())
            .AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
            .ToString() ?? "Not Found";
    }
    
    #region WaitingRoomLogic
    public void ConnectToLobby()
    {
        Debug.Log("Host starts waiting room for lobby");
        string ipAddress = codeInputField.text;
        if (string.IsNullOrWhiteSpace(ipAddress)) return;


        // Klijent postavlja IP adresu na koju se želi spojiti. Port mora biti isti kao kod hosta.
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            ipAddress,
            7777
        );

        // StartClient() pokušava se spojiti na server na prethodno postavljenoj IP adresi i portu.
        NetworkManager.Singleton.StartClient();
        waitingRoomScreen.transform.GetChild(4).GetComponent<TMP_Text>().color = new Color(0, 256, 0);
        joinButton.interactable = false;

    }
    private void StartWaitingRoom() // call when switching from main menu to waiting room
    {
        mainMenuScreen.SetActive(false);
        waitingRoomScreen.SetActive(true);
    }
    [ClientRpc]
    private void ForceExitClientsClientRpc() // force clients to exit the server, kick them out of the waiting room
    {
        ExitToMainMenu();
    }
    private void StartLobby() //starts the lobby room (no players can join after that point)
    {
        UIChangeClientRpc();
    }
    private void ExitToMainMenu() //pressing the X returns you back to the main menu
    {
        if (waitingRoomScreen.activeInHierarchy)
        {
            waitingRoomScreen.SetActive(false);
            mainMenuScreen.SetActive(true);
        }
        else if (gameOverScreen.activeInHierarchy)
        {
            gameOverScreen.SetActive(false);
            mainMenuScreen.SetActive(true);
        }
        ReturnToMainMenu();
    }
    private void ReturnToMainMenu() // main function for handling exits from the waiting room 
                                    // and disconnecting people from the hosts server
    {
        if (IsHost)
        {
            ForceExitClientsClientRpc();
            Debug.Log("Host shutting down server.");
            joinCodeText.text = "";
            numberOfConnectedPlayers.text = "1";
            for (int i = 0; i < 7; i++)
            {
                waitingRoomScreen.transform.GetChild(i).gameObject.SetActive(false);
            }
            NetworkManager.Singleton.Shutdown();
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Client disconnecting.");
            codeInputField.text = "";
            waitingRoomScreen.transform.GetChild(4).GetComponent<TMP_Text>().color = new Color(256, 0, 0);
            for (int i = 4; i < 7; i++)
            {
                waitingRoomScreen.transform.GetChild(i).gameObject.SetActive(false);
            }
            joinButton.interactable = true;
            NetworkManager.Singleton.Shutdown();
        }
    }
    #endregion
}