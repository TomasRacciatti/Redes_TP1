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
    [Networked] public int currentTurnId { get; set; }
    
    private List<PlayerController> _players = new List<PlayerController>();
    
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
        if (!_players.Contains(player))
        {
            _players.Add(player);
            Debug.Log($"[GameManager] Registered player {player.Object.InputAuthority}");
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
    
    private void AssignTurnIDs()
    {
        int id = 1;
        foreach (var player in _players)
        {
            player.myTurnId = id++;
        }
    }

    private IEnumerator DelayedStart()
    {
        yield return null;

        AssignTurnIDs();
        StartGame();
    }

    private void StartGame()
    {
        Debug.Log("Game started!");

        currentClaimQuantity = 0;
        currentClaimFace = 1;
        
        var alivePlayers = _players.Where(p => p.IsAlive).ToList();
        var randomPlayer = alivePlayers[UnityEngine.Random.Range(0, alivePlayers.Count)];
        currentTurnId = randomPlayer.myTurnId;

        foreach (var player in _players)
        {
            player.RollDice();
        }

        Runner.StartCoroutine(DelayedUIUpdate());
    }
    
    private IEnumerator DelayedUIUpdate()
    {
        yield return new WaitForSeconds(0.1f);

        UpdateUI();
    }

    public void NextTurn()
    {
        var alivePlayers = _players.Where(p => p.IsAlive).OrderBy(p => p.myTurnId).ToList();
        int currentIndex = alivePlayers.FindIndex(p => p.myTurnId == currentTurnId);
        int nextIndex = (currentIndex + 1) % alivePlayers.Count;

        currentTurnId = alivePlayers[nextIndex].myTurnId;
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        UIManager.Instance.UpdateTurnIndicator();
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
        Dictionary<int, int> distribution = new();

        foreach (var player in _players)
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
