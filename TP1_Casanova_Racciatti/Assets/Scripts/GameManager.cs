using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Networked] public int currentClaimQuantity { get; set; }
    [Networked] public int currentClaimFace { get; set; }
    [Networked] private PlayerRef currentTurnPlayer { get; set; }

    private Dictionary<PlayerRef, PlayerController> _players = new();
    private bool _gameStarted;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        else
            Destroy(gameObject);
    }

    public void RegisterPlayer(PlayerController player)
    {
        var playerRef = player.Object.InputAuthority;
        
        if (!_players.ContainsKey(playerRef))
        {
            _players.Add(playerRef, player);
        }

        TryStartGame();
    }

    private void TryStartGame()
    {
        if (_gameStarted) return;

        if (_players.Count >= 2)
        {
            _gameStarted = true;
            Runner.StartCoroutine(DelayedStart());
        }
    }

    private IEnumerator DelayedStart()
    {
        yield return null; 

        StartGame();
    }

    private void StartGame()
    {
        Debug.Log("Game started!");

        currentClaimQuantity = 0;
        currentClaimFace = 1;
        
        var alive = _players.Keys.ToList();
        currentTurnPlayer = alive[UnityEngine.Random.Range(0, alive.Count)];

        foreach (var player in _players.Values)
        {
           player.RollDice(); 
        }

        UpdateUI();
    }

    public void NextTurn()
    {
        var alivePlayers = _players
            .Where(pair => pair.Value.IsAlive)
            .Select(pair => pair.Key)
            .ToList();

        int currentIndex = alivePlayers.IndexOf(currentTurnPlayer);
        int nextIndex = (currentIndex + 1) % alivePlayers.Count;

        currentTurnPlayer = alivePlayers[nextIndex];
        UpdateUI();
    }

    public int GetPlayerIndex(PlayerController player)
    {
        int index = 0;
        foreach (var entry in _players)
        {
            if (entry.Value == player)
                return index;

            index++;
        }

        return -1; 
    }
    
    public PlayerController GetCurrentPlayer()
    {
        return _players.TryGetValue(currentTurnPlayer, out var player) ? player : null;
    }

    private void UpdateUI()
    {
        UIManager.Instance.UpdateTurnIndicator(GetCurrentPlayer());
        UIManager.Instance.UpdateClaim(currentClaimQuantity, currentClaimFace);
        UIManager.Instance.UpdateDiceCounts(_players);
        UIManager.Instance.UpdateCurrentClaimDie(currentClaimFace);
    }
    
    public void SetClaim(int quantity, int face)
    {
        currentClaimQuantity = quantity;
        currentClaimFace = face;
        UpdateUI();
        NextTurn();
    }
    
    private Dictionary<int, int> GetDiceDistribution()
    {
        Dictionary<int, int> distribution = new Dictionary<int, int>();

        foreach (var player in _players.Values)
        {
            if (!player.IsAlive) continue;

            foreach (int face in player.RolledDice)
            {
                if (!distribution.ContainsKey(face))
                    distribution[face] = 0;

                distribution[face]++;
            }
        }

        return distribution;
    }
}
