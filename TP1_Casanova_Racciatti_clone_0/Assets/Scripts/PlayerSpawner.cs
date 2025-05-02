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
        //Debug.Log($"[Spawner] PlayerJoined: {player} | Local: {Runner.LocalPlayer}");
        if (_hasSpawned)
        {
            //Debug.Log("[Spawner] Already spawned.");
            return;
        }

        if (player == Runner.LocalPlayer)
        {
            //Debug.Log("[Spawner] Spawning local player.");
            
            Runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, inputAuthority: Runner.LocalPlayer);
            _hasSpawned = true;
        }
    }
}
