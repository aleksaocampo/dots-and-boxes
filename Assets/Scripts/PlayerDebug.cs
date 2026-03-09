using Unity.Netcode;
using UnityEngine;

/**
PLAYER DEBUG:
used for checking if a client has joined
**/

public class PlayerDebug : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            Debug.Log($"Player {OwnerClientId} joined");
    }
}
