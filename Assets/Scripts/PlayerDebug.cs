using Unity.Netcode;
using UnityEngine;

public class PlayerDebug : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            Debug.Log($"Player {OwnerClientId} spawned");
    }
}
