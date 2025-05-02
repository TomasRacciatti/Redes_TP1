using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] NetworkPrefabRef _playerPrefab;

    private bool _hasSpawned;
    
    public void PlayerJoined(PlayerRef player)
    {
        if (_hasSpawned)
            return;

        if (player != Runner.LocalPlayer)
            return;

        if (Runner.SessionInfo.PlayerCount < 2)
        {
            Debug.Log("Waiting for more players");
            return;
        }

        Runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, inputAuthority: Runner.LocalPlayer);
        _hasSpawned = true;
        
        Debug.Log("Player spawned");
    }
}
