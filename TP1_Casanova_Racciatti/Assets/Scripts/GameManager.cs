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

    public int currentTurnId { get; set; }

    private int _lastTurnID;
    private PlayerController LastPlayer => _players.First(p => p.myTurnId == _lastTurnID);

    private List<PlayerController> _players = new List<PlayerController>();
    public IReadOnlyList<PlayerController> Players => _players;

    private bool _isFirstTurn;
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
            //Debug.Log($"[GameManager] Registered player {player.Object.InputAuthority}");
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

        var championRaw = _players
            .Min(p => p.Object.InputAuthority.RawEncoded);

        if (Runner.LocalPlayer.RawEncoded == championRaw)
        {
            var alive = _players.Where(p => p.IsAlive)
                .OrderBy(p => p.Object.InputAuthority.RawEncoded)
                .ToList();

            int idx = UnityEngine.Random.Range(0, alive.Count);
            var first = alive[idx];

            RPC_StartGame(first.Object.InputAuthority, first.myTurnId);
        }
    }

    private void StartRound(PlayerRef firstAuthority, int firstTurnId)
    {
        UIManager.Instance.HideRoundSummary();
        
        Debug.Log("Round started!");

        _isFirstTurn = true;
        currentClaimQuantity = 0;
        currentClaimFace = 1;

        turnAuthority = firstAuthority;
        currentTurnId = firstTurnId;

        foreach (var player in _players)
        {
            player.RollDice();
        }

        UIManager.Instance.UpdateDiceCounts(_players);
        UpdateUI();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_StartGame(PlayerRef firstAuthority, int firstTurnId)
    {
        StartRound(firstAuthority, firstTurnId);
    }

    public void RequestNextTurn()
    {
        if (Runner.LocalPlayer != turnAuthority)
            return;

        RPC_AdvanceTurn();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_AdvanceTurn()
    {
        //Debug.Log($"[RPC_AdvanceTurn] invoked on {Runner.LocalPlayer} — old turn: {currentTurnId}");

        var alive = _players
            .Where(p => p.IsAlive)
            .OrderBy(p => p.Object.InputAuthority.RawEncoded)
            .ToList();

        int index = alive.FindIndex(p => p.myTurnId == currentTurnId);
        int nextIdx = (index + 1) % alive.Count;
        var next = alive[nextIdx];

        turnAuthority = next.Object.InputAuthority;
        currentTurnId = next.myTurnId;

        UpdateUI();
    }

    private void UpdateUI()
    {
        UIManager.Instance.UpdateTurnIndicator();
        UIManager.Instance.UpdateClaim(currentClaimQuantity, currentClaimFace);
        //UIManager.Instance.UpdateDiceCounts(_players);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetClaim(int quantity, int face)
    {
        currentClaimQuantity = quantity;
        currentClaimFace = face;
        _isFirstTurn = false;
        _lastTurnID = currentTurnId;

        RequestNextTurn();
    }

    public void CallBluff() // Lo llamamos en el boton
    {
        if (_isFirstTurn)
            return;
        
        RPC_ResolveBluff();
    }

    [Rpc(RpcSources.All, RpcTargets.Proxies)]
    private void RPC_ResolveBluff()
    {
        var dist = GetDiceDistribution();
        dist.TryGetValue(currentClaimFace, out int actualCount);

        var caller = _players.First(p => p.myTurnId == currentTurnId);
        var claimant = LastPlayer;

        bool honest = actualCount >= currentClaimQuantity;
        var loser = honest ? caller : claimant;
        
        
        loser.LoseOneDie();

        if (Runner.LocalPlayer != loser.Object.InputAuthority)
            loser.RPC_LoseOneDieLocal();

        if (HasStateAuthority)
            //StartCoroutine(RoundSummaryRoutine(loser.myTurnId, dist));

        RPC_StartGame(loser.Object.InputAuthority, loser.myTurnId);
    }
    
    private IEnumerator RoundSummaryRoutine(int loserID, Dictionary<int, int> diceDistribution)
    {
        int claimQty  = currentClaimQuantity;
        int claimFace = currentClaimFace;
        
        RPC_ShowRoundSummary(diceDistribution, claimQty, claimFace, loserID);
        
        yield return new WaitForSeconds(3f);
    }
    
    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowRoundSummary(Dictionary<int, int> diceDistribution, int claimQty, int claimFace, int loserId)
    {
        UIManager.Instance.ShowRoundSummary(diceDistribution, claimQty, claimFace, loserId);
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

        /*// DEBUG para revisar proxies
        var sb = new System.Text.StringBuilder("[GetDiceDistribution] ");
        foreach (var kv in distribution)
            sb.Append($"{kv.Key}→{kv.Value}  ");
        Debug.Log(sb.ToString());
        */

        return distribution;
    }

    
    
    private void RemoveFromList(PlayerController player)
    {
        _players.Remove(player);
    }
    
    [Rpc]
    public void RPC_GameOver(PlayerRef client)
    {
        if (client == Runner.LocalPlayer)
        {
            UIManager.Instance.ShowDefeatOverlay();
        }

        var player = GetPlayerController(client);
        RemoveFromList(player);

        if (_players.Count == 1 && HasStateAuthority)
        {
            RPC_Win(_players[0]);
        }
    }
    
    [Rpc]
    private void RPC_Win(PlayerController player)
    {
        UIManager.Instance.ShowVictoryOverlay();
    }
    
    // Helper para obtener el PLayerController desde playerRef
    private PlayerController GetPlayerController(PlayerRef client)
    {
        return _players.FirstOrDefault(p => p.Object.InputAuthority == client);
    }
}