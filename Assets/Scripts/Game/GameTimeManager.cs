using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameTimeManager : NetworkBehaviour
{
    [SerializeField] LobbyManager lobby;
    [SerializeField] GameUI gameUI;
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
}
