using System.Collections.Generic;
using System;
using Unity.Netcode;

public class VoteManager : NetworkBehaviour
{
    public static VoteManager Instance;

    private Dictionary<ulong, ulong?> votes = new();
    private bool votingActive;

    private void Awake()
    {
        Instance = this;
    }
}
