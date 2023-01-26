using System;
using System.Collections.Generic;
using Alteruna;
using Alteruna.Trinity;
using UnityEngine;

public class GameState : MonoBehaviour
{

    private List<ushort> alivePlayers;
 
    private void Start()
    {
        alivePlayers = new List<ushort>();
        Lobby.Instance.Multiplayer.RegisterRemoteProcedure("Decrement_Num_Players", Decrement_Num_Players);
    }

    private void OnEnable()
    {
        Lobby.Instance.OnStartMatch.AddListener(SetPlayers);
        HealthComponent.OnDeath += PlayerDied;
    }

    private void OnDisable()
    {
        Lobby.Instance.OnStartMatch.RemoveListener(SetPlayers);
        HealthComponent.OnDeath -= PlayerDied;
    }

    private void SetPlayers()
    {
        alivePlayers.Clear();
        foreach (var playerData in Lobby.Instance.PlayersData)
        {
            if (playerData.User == null) continue;
             
            alivePlayers.Add(playerData.User);
            Lobby.Instance.MessageLobby("added user: " + playerData.User.Index);
            Debug.Log("adding user local: " + playerData.User.Index);
        }
    }

    public void PlayerDied(ushort id)
    {
        Debug.Log("trying to remove local: " + id);
        Lobby.Instance.MessageLobby("trying to remove user: " + id);
        alivePlayers.Remove(id);
        
        Lobby.Instance.MessageLobby("num players alive: " + alivePlayers.Count);
        Debug.Log("local num alive: " + alivePlayers.Count);

        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("user", id);
        
        Lobby.Instance.Multiplayer.InvokeRemoteProcedure("Decrement_Num_Players", UserId.All, parameters);
        
        if (!Lobby.Instance.IsAdmin()) return;
        
        if (alivePlayers.Count <= 1)
        {
            Lobby.Instance.MessageLobby("Game Over");
            Lobby.Instance.EndMatch();
        }
        
    }

    private void Decrement_Num_Players(ushort fromUser, ProcedureParameters parameters, uint callId,
        ITransportStreamReader processor)
    {
        ushort userID = parameters.Get("user", (ushort)666);

        User user = Lobby.Instance.Multiplayer.GetUser(userID);
        
        alivePlayers.Remove(userID);
        Lobby.Instance.GetPlayer(user).GetComponent<HealthComponent>().DisablePlayer();
        
        if (!Lobby.Instance.IsAdmin()) return;
        
        if (alivePlayers.Count <= 1)
        {
            Lobby.Instance.MessageLobby("Game Over");
            Lobby.Instance.EndMatch();
        }
    }
    
}
