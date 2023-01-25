using System;
using System.Collections;
using System.Collections.Generic;
using Alteruna;
using UnityEngine;

public class GameState : MonoBehaviour
{

    private int numPlayers;
    
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

    private void PlayerDied()
    {
        numPlayers--;
        if (numPlayers <= 1)
        {
            Lobby.Instance.MessageLobby("Game Over");
            Lobby.Instance.EndMatch();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
