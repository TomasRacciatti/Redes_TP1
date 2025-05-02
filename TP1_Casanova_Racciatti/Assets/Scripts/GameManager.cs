using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Networked] public int currentClaimQuantity { get; set; }
    [Networked] public int currentClaimFace { get; set; }
    [Networked] public int currentTurnIndex { get; set; }

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
        }

        TryStartGame();
    }

    private void TryStartGame()
    {
        if (_gameStarted) return;

        if (_players.Count >= 2)
        {
            _gameStarted = true;
            StartGame();
        }
    }

    private void StartGame()
    {
        Debug.Log("Game started!");

        currentClaimQuantity = 0;
        currentClaimFace = 1;
        
        currentTurnIndex = UnityEngine.Random.Range(0, _players.Count);

        foreach (var player in _players)
        {
           player.RollDice(); 
        }

        UpdateUI();
    }

    public void NextTurn()
    {
        do
        {
            currentTurnIndex = (currentTurnIndex + 1) % _players.Count;
        } while (!_players[currentTurnIndex].IsAlive);

        UpdateUI();
    }

    public int GetPlayerIndex(PlayerController player)
    {
        return _players.IndexOf(player);
    }
    
    public PlayerController GetCurrentPlayer()
    {
        return _players[currentTurnIndex];
    }

    private void UpdateUI()
    {
        UIManager.Instance.UpdateTurnIndicator(GetCurrentPlayer());
        UIManager.Instance.UpdateClaim(currentClaimQuantity, currentClaimFace);
        UIManager.Instance.UpdateDiceCounts(_players);
    }
    
    public void SetClaim(int quantity, int face)
    {
        currentClaimQuantity = quantity;
        currentClaimFace = face;
        UpdateUI();
        NextTurn();
    }
}
