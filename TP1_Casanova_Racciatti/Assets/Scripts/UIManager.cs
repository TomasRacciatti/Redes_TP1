using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("Player Dice UI")]
    [SerializeField] private List<DieDisplay> _rolledDiceDisplays;

    [Header("Claim Dice UI")]
    [SerializeField] private DieDisplay _currentClaimDie;
    [SerializeField] private DieDisplay _raiseClaimDie;
    
    [Header("Claim")]
    [SerializeField] private TextMeshProUGUI _claimAmountText;
    [SerializeField] private DieDisplay _claimDie;
    
    [Header("Turn")]
    [SerializeField] private GameObject _turnOverlay;
    [SerializeField] private TextMeshProUGUI _turnText;
    [SerializeField] private GameObject _actionButtons;

    [Header("Players")]
    [SerializeField] private TextMeshProUGUI _playerListText;
    
    private PlayerController _localPlayer;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }

    public void SetPlayerReference(PlayerController player)
    {
        _localPlayer = player;
    }
    
    public void UpdateClaim(int quantity, int face)
    {
        _claimAmountText.text = quantity.ToString();
    }

    public void UpdateTurnIndicator(PlayerController currentPlayer)
    {
        bool isMyTurn = currentPlayer == _localPlayer;
        
        _turnOverlay.SetActive(!isMyTurn);
        _actionButtons.SetActive(isMyTurn);
        
        _turnText.text = $"Player{GameManager.Instance.GetPlayerIndex(currentPlayer) + 1}'s turn";
    }

    public void UpdateDiceCounts(List<PlayerController> players)
    {
        _playerListText.text = "";
        
        for (int i = 0; i < players.Count; i++)
        {
            _playerListText.text += $"Player{i + 1}: {players[i].RemainingDice}\n";
        }
    }
    
    public void UpdateRolledDice(List<int> rolledValues)
    {
        for (int i = 0; i < _rolledDiceDisplays.Count; i++)
        {
            if (i < rolledValues.Count)
                _rolledDiceDisplays[i].ShowValue(rolledValues[i]);
        }
    }

    public void UpdateCurrentClaimDie(int value)
    {
        _currentClaimDie.ShowValue(value);
    }

    public void UpdateRaiseClaimDie(int value)
    {
        _raiseClaimDie.ShowValue(value);
    }
}
