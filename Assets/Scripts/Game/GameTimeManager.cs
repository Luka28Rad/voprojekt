using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameTimeManager : NetworkBehaviour
{
    [SerializeField] LobbyManager lobby;
    public NetworkVariable<float> ShowcaseTime = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
    );
    [SerializeField] private float roleShowcaseDuration;
    public void StartShowcaseTimer()
    {
        if (IsServer)
        {
            ShowcaseTime.Value = roleShowcaseDuration;
            Debug.Log("TIMER STARTED");
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
}
