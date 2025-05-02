using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Dice Settings")]
    [SerializeField] private int _maxDice = 5;

    [Networked] public int RemainingDice { get; set; }
    
    public List<int> RolledDice { get; private set; } = new List<int>();

    public bool IsAlive => RemainingDice > 0;
    
    [Networked] public int myTurnId { get; set; }
    

    public override void Spawned()
    {
        for (int i = 0; i < _maxDice; i++)
        {
            RolledDice.Add(1);
        }
        
        RemainingDice = _maxDice;

        if (HasStateAuthority)
        {
            Debug.Log($"[PlayerController] I am the local player. My NetworkObject ID: {Object.Id}");
            Debug.Log($"[PlayerController] My TurnID is: {myTurnId}");
            UIManager.Instance.SetPlayerReference(this);
        }
        GameManager.Instance.RegisterPlayer(this);
    }

    public void RollDice()
    {
        RolledDice.Clear();
        for (int i = 0; i < RemainingDice; i++)
        {
            RolledDice.Add(UnityEngine.Random.Range(1, 7));
        }
        
        if (HasInputAuthority)
            UIManager.Instance.UpdateRolledDice(RolledDice);
    }
    
    public void LoseOneDie()
    {
        RemainingDice--;
        
        // Agregar logica de desactivar los dados
    }
}
