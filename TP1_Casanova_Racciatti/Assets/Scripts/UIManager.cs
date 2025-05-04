using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("Player Dice UI")]
    [SerializeField] private List<DieDisplay> _rolledDiceDisplays;

    [Header("Claim Dice UI")]
    [SerializeField] private DieDisplay _currentClaimDie;
    //[SerializeField] private DieDisplay _raiseClaimDie;
    
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
        //Debug.Log($"[UIManager] Local player set to turnId={player.myTurnId}");
    }
    
    public void UpdateClaim(int quantity, int face)
    {
        _claimAmountText.text = quantity.ToString();
        _currentClaimDie.ShowValue(face);
    }

    public void UpdateTurnIndicator()
    {
        if (_localPlayer == null) return;

        bool isMyTurn = _localPlayer.myTurnId == GameManager.Instance.currentTurnId;

        //Debug.Log($"[UIManager] MyTurnId: {_localPlayer.myTurnId}, CurrentTurnId: {GameManager.Instance.currentTurnId}, IsMyTurn: {isMyTurn}");

        _turnOverlay.SetActive(!isMyTurn);
        _actionButtons.SetActive(isMyTurn);

        _turnText.text = $"Player {GameManager.Instance.currentTurnId}'s turn";
    }

    public void UpdateDiceCounts(List<PlayerController> players)
    {
        _playerListText.text = "";
        
        //var ordered = players.OrderBy(p => p.myTurnId);

        for (int i = 0; i < players.Count; i++)
        {
            _playerListText.text += $"Player{i + 1}: {players[i].RemainingDice}\n";
        }
    }
    
    public void UpdateRolledDice(List<int> rolledValues)
    {
        for (int i = 0; i < _rolledDiceDisplays.Count; i++)
        {
            bool slotActive = i < rolledValues.Count;
            
            _rolledDiceDisplays[i].gameObject.SetActive(slotActive);
            
            if (slotActive)
                _rolledDiceDisplays[i].ShowValue(rolledValues[i]);
        }
    }
}
