using System.Net;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

// Nasljeđuje NetworkBehaviour, što je osnova za sve skripte koje trebaju mrežnu funkcionalnost u Netcode for GameObjects (NGO).
// To nam daje pristup svojstvima poput IsHost, IsServer, IsClient i mrežnim metodama.
public class LobbyManager : NetworkBehaviour
{
    private static int MIN_PLAYER_TO_START_GAME = 2;
    public static LobbyManager Instance { get; private set; }
    public static string PlayerName { get; private set; }

    [Header("Screens")]
    [SerializeField] private GameObject mainMenuScreen;
    [SerializeField] private GameObject joinGameScreen;
    [SerializeField] private GameObject lobbyScreen;

    [Header("Main Menu Screen")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button connectButton;

    [Header("Join Game Screen")]
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private Button joinButton;

    [Header("Lobby Screen")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Transform playerCardsContainer;
    [SerializeField] private GameObject playerCardPrefab;

    [Header("Game Screen")]
    [SerializeField] private GameObject gameScreen;

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

    private void Start()
    {
        hostButton.onClick.AddListener(HostLobby);
        connectButton.onClick.AddListener(ShowJoinScreen);
        joinButton.onClick.AddListener(ConnectToLobby);
        startGameButton.onClick.AddListener(StartGame);
        
        nameInputField.onValueChanged.AddListener(OnNameChanged);

        hostButton.interactable = false;
        connectButton.interactable = false;

        mainMenuScreen.SetActive(true);
        joinGameScreen.SetActive(false);
        lobbyScreen.SetActive(false);
        gameScreen.SetActive(false);
    }

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

    private void SavePlayerName()
    {
        PlayerName = nameInputField.text;
    }

    private void StartGame()
    {
        // IsHost je svojstvo iz NetworkBehaviour-a koje je istinito samo na računalu koje je pokrenulo igru kao host.
        // Osiguravamo da samo host može započeti igru, što je standardna praksa za autoritativni model.
        if (!IsHost) return;
        
        GameManager.Instance.StartGameAndAssignRoles();
    }

    public void HostLobby()
    {
        SavePlayerName();

        // Postavljamo podatke za Unity Transport sloj. Ovdje definiramo IP adresu i port
        // na kojem će server "slušati" dolazeće konekcije. "0.0.0.0" znači "sve dostupne mreže".
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            "0.0.0.0",
            7777
        );

        // StartHost() pokreće i server i klijenta na istom računalu.
        // Osoba koja ovo pokrene je i server i igrač (client).
        NetworkManager.Singleton.StartHost();
        
        mainMenuScreen.SetActive(false);
        lobbyScreen.SetActive(true);
        joinCodeText.text = $"IP: {GetLocalIPv4()}";
    }

    public void ShowJoinScreen()
    {
        SavePlayerName();
        mainMenuScreen.SetActive(false);
        joinGameScreen.SetActive(true);
    }

    public void ShowGameScreen()
    {
        lobbyScreen.SetActive(false);
        gameScreen.SetActive(true);
    }

    public void ConnectToLobby()
    {
        string ipAddress = codeInputField.text;
        if (string.IsNullOrWhiteSpace(ipAddress)) return;

        // Klijent postavlja IP adresu na koju se želi spojiti. Port mora biti isti kao kod hosta.
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            ipAddress,
            7777
        );

        // StartClient() pokušava se spojiti na server na prethodno postavljenoj IP adresi i portu.
        NetworkManager.Singleton.StartClient();
    }

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
            joinGameScreen.SetActive(false);
            lobbyScreen.SetActive(true);
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
            GameObject card = Instantiate(playerCardPrefab, playerCardsContainer);
            
            // Svaki klijent na mreži ima svoj PlayerObject, koji je NetworkObject koji ga predstavlja u igri.
            var playerNetworkData = client.PlayerObject.GetComponent<PlayerNetworkData>();
            
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
}