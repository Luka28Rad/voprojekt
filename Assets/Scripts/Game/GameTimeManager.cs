using NUnit.Framework.Constraints;
using System.Collections;
using System.Threading;
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
            if (false) //nothing burger day
                child_index = 0;
            else //someone was killed
                child_index = 1;

            //
            //Patrick promjene
            //Dohvati lobbyManager i uzmi objekt koji ima kartice igraca
            LobbyManager lobbyManager = FindObjectOfType<LobbyManager>(); //Nije vise potrebno, neka ostane
            GameObject playerCardsContainer = lobbyManager.playerContainer; //Nije vise potrebno, neka ostane
            FindAndReparentDeadPlayer(playerCardsContainer.transform);
            //End Patrick promjena
            //

            newspaperScreen.GetChild(child_index).gameObject.SetActive(true);
            news = newspaperScreen.GetChild(child_index).gameObject.GetComponent<NewspaperAnimation>();
            news.PlayAnimation();
            yield return new WaitForSeconds(newspaperDuration-news.duration);
            newspaperScreen.GetChild(child_index).gameObject.SetActive(false);
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
        if (panelIndex == 0)
        {
            int cart = lobby.GetPlayerModel().GetComponent<EditPlayerLook>().networkData.TrainCart.Value;
            var bg_image = gameUI.nightTimeScreen.GetComponent<Image>();
            var armrest_image = gameUI.nightTimeScreen.transform.GetChild(0).GetComponent<Image>();
            if (cart == 1)
            {
                bg_image.sprite = cart1;
                armrest_image.sprite = armrest1;
            }
            else if (cart == 2)
            {
                bg_image.sprite = cart2;
                armrest_image.sprite = armrest2;
            }
            else if (cart == 3)
            {
                bg_image.sprite = cart3;
                armrest_image.sprite = armrest3;
            }
            gameUI.nightTimeScreen.SetActive(true);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                StartNightTimeTimer();
        }
        else if (panelIndex == 1)
        { 
            gameUI.dayTimeScreen.SetActive(true);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                StartDayTimeTimer();
        }
        transitionRoutine = null;
    }

    //Ovo za sada dohvati prvog u listi kojeg najde da ima dead = true, kada dodamo onaj kao stog ili sto vec sa svim mrtvima treba promjeniti da
    //uzme prvog od tih kojeg najde
    private void FindAndReparentDeadPlayer(Transform parent)
    {
        EditPlayerLook[] allCards = FindObjectsOfType<EditPlayerLook>(true);

        foreach (var card in allCards)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var data = client.PlayerObject.GetComponent<PlayerNetworkData>();
                if (card.linkedClientId == client.ClientId && data.dead.Value)
                {
                    Transform deadParent = gameUI.transitionScreen.transform;

                   //Ovo dojde do onog elementa gdje se nalazi kao mjesto na novinama za mrtvog igraca
                    for (int i = 0; i < 3; i++)
                    {
                        deadParent = deadParent.GetChild(deadParent.childCount - 1);
                    }

                    //Reparenta karticu
                    card.transform.SetParent(deadParent, false);
                    card.transform.localPosition = Vector3.zero;
                    card.transform.SetAsLastSibling();
                    return;
                }
            }
        }
    }
}
