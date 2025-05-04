using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Dice Settings")] [SerializeField]
    private int _maxDice = 5;
    
    [Networked] public int RemainingDice { get; set; }

    public List<int> RolledDice { get; private set; } = new List<int>();

    public bool IsAlive => RemainingDice > 0;

    [Networked, OnChangedRender(nameof(OnTurnIdChanged))]
    public int myTurnId { get; set; }


    public override void Spawned()
    {
        for (int i = 0; i < _maxDice; i++)
        {
            RolledDice.Add(1);
        }

        RemainingDice = _maxDice;

        if (HasInputAuthority)
        {
            //Debug.Log($"[PlayerController] I am the local player. My NetworkObject ID: {Object.Id}");
            UIManager.Instance.SetPlayerReference(this);
        }

        GameManager.Instance.RegisterPlayer(this);
    }

    private void Update() // Para testear si cambia de turno
    {
        if (HasInputAuthority && Input.GetKeyDown(KeyCode.Space))
        {
            if (myTurnId == GameManager.Instance.currentTurnId)
            {
                Debug.Log("[PlayerController] Space pressed — requesting next turn.");
                GameManager.Instance.RequestNextTurn();
            }
            else
            {
                Debug.Log("[PlayerController] Not my turn. Ignoring space.");
            }
        }
    }

    private void OnTurnIdChanged()
    {
        Debug.Log($"[PlayerController] My TurnID is: {myTurnId}");
        if (HasInputAuthority)
        {
            UIManager.Instance?.UpdateTurnIndicator();
        }
    }

    public void RollDice()
    {
        if (!HasInputAuthority)
            return;
        
        RolledDice.Clear();
        for (int i = 0; i < RemainingDice; i++)
        {
            RolledDice.Add(UnityEngine.Random.Range(1, 7));
        }
        
        RPC_SyncRolledDice(RolledDice.ToArray());

        if (HasInputAuthority)
            UIManager.Instance.UpdateRolledDice(RolledDice);
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.Proxies)]
    private void RPC_SyncRolledDice(int[] diceValues)
    {
        RolledDice = new List<int>(diceValues);
    }

    public void LoseOneDie()
    {
        if (RemainingDice <= 0)
        {
            return; // GAME OVER
        }
            

        RemainingDice--;

        if (RolledDice.Count > RemainingDice)
            RolledDice.RemoveAt(RolledDice.Count - 1);

        if (HasInputAuthority)
        {
            UIManager.Instance.UpdateRolledDice(RolledDice);
        }
    }
}