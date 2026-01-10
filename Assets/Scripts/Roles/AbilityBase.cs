using Unity.Netcode;
using UnityEngine;

public abstract class AbilityBase : NetworkBehaviour
{
    [SerializeField] protected float range = 10f;
    protected PlayerNetworkData localData;

    public override void OnNetworkSpawn()
    {
        localData = GetComponent<PlayerNetworkData>();
        localData.OnRoleAssigned += HandleRoleAssigned;
        this.enabled = false;
    }

    protected abstract void HandleRoleAssigned(PlayerRole role);

    protected virtual void Update()
    {
        if (!IsOwner || !localData.IsAlive.Value) return;
    }
}