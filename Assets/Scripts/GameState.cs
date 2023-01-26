using System;
using Alteruna;
using Alteruna.Trinity;
using UnityEngine;

public class GameState : MonoBehaviour
{

    private int numPlayers;

    private void Start()
    {
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
        // called after game has been inited and player data verified in Lobby
        // so we can be sure PlayersData == how many players.
        numPlayers = Lobby.Instance.PlayersData.Count;
    }

    public void PlayerDied()
    {
        numPlayers--;

        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("numPlayers", numPlayers);
        Lobby.Instance.Multiplayer.InvokeRemoteProcedure("Decrement_Num_Players", UserId.All, parameters);
        
        if (!Lobby.Instance.IsAdmin()) return;
        
        if (numPlayers <= 1)
        {
            Lobby.Instance.MessageLobby("Game Over");
            Lobby.Instance.EndMatch();
        }
    }

    private void Decrement_Num_Players(ushort fromUser, ProcedureParameters parameters, uint callId,
        ITransportStreamReader processor)
    {
        numPlayers = parameters.Get("numPlayers", 4);
    }
    
}
