using System;
using System.Collections.Generic;
using Alteruna;
using Alteruna.Trinity;
using UnityEngine;

public class GameState : MonoBehaviour
{

    private List<User> alivePlayers;
 
    private void Start()
    {
        alivePlayers = new List<User>();
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
        foreach (var playerData in Lobby.Instance.PlayersData)
        {
            alivePlayers.Add(playerData.User);
        }
    }

    public void PlayerDied(User user)
    {
        alivePlayers.Remove(user);
        
        Lobby.Instance.MessageLobby("num players alive: " + alivePlayers.Count);

        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("user", user.Index);
        
        if (!Lobby.Instance.IsAdmin()) return;
        
        if (alivePlayers.Count <= 1)
        {
            Lobby.Instance.MessageLobby("Game Over");
            Lobby.Instance.EndMatch();
        }
        
        Lobby.Instance.Multiplayer.InvokeRemoteProcedure("Decrement_Num_Players", UserId.All, parameters);
    }

    private void Decrement_Num_Players(ushort fromUser, ProcedureParameters parameters, uint callId,
        ITransportStreamReader processor)
    {
        ushort userID = parameters.Get("user", (ushort)0);

        User user = Lobby.Instance.Multiplayer.GetUser(userID);
        
        alivePlayers.Remove(user);
        Lobby.Instance.GetPlayer(user).GetComponent<HealthComponent>().DisablePlayer();
        
        if (!Lobby.Instance.IsAdmin()) return;
        
        if (alivePlayers.Count <= 1)
        {
            Lobby.Instance.MessageLobby("Game Over");
            Lobby.Instance.EndMatch();
        }
    }
    
}
