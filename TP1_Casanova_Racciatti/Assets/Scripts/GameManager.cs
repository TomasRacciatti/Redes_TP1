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
    
    public PlayerRef turnAuthority { get; set; }
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
        if (_gameStarted || _players.Count < 2) return;
        _gameStarted = true;
        StartCoroutine(DelayedStart());
    }

    private void AssignTurnIDs()
    {
        var ordered = _players
            .Where(p => p.IsAlive)
            .OrderBy(p => p.Object.InputAuthority.RawEncoded)
            .ToList();

        int id = 1;
        foreach (var p in ordered)
            p.myTurnId = id++;
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

        var alive = _players.Where(p => p.IsAlive).ToList();
        var first = alive[UnityEngine.Random.Range(0, alive.Count)];
        
        turnAuthority  = first.Object.InputAuthority;
        currentTurnId  = first.myTurnId;

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

    private void OnTurnAuthorityChanged() // No deberia usar RCP?
    {
        UIManager.Instance.UpdateTurnIndicator();
    }
    
    public void RequestNextTurn()
    {
        if (Runner.LocalPlayer != turnAuthority)
            return;

        RPC_AdvanceTurn();
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AdvanceTurn()
    {
        var alive = _players
            .Where(p => p.IsAlive)
            .OrderBy(p => p.Object.InputAuthority.RawEncoded)
            .ToList();

        int index    = alive.FindIndex(p => p.myTurnId == currentTurnId);
        int nextIdx  = (index + 1) % alive.Count;
        var next     = alive[nextIdx];

        turnAuthority = next.Object.InputAuthority;
        currentTurnId = next.myTurnId;
        
        UpdateUI();
    }
    
    /*
    public void NextTurn()
    {
        if (Runner.LocalPlayer != turnAuthority)
            return;

        var alive = _players
            .Where(p => p.IsAlive)
            .OrderBy(p => p.Object.InputAuthority.RawEncoded)
            .ToList();

        int idx      = alive.FindIndex(p => p.myTurnId == currentTurnId);
        int nextIdx  = (idx + 1) % alive.Count;
        var next     = alive[nextIdx];

        turnAuthority  = next.Object.InputAuthority;
        currentTurnId  = next.myTurnId;
        
        UpdateUI();
    }
    */

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
        RequestNextTurn();
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