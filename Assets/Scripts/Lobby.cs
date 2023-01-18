using Alteruna;
using Alteruna.Trinity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Multiplayer), typeof(Spawner))]
public class Lobby : MonoBehaviour
{
    public static Lobby Instance;

    [NonSerialized]
    public Multiplayer Multiplayer;

    [NonSerialized]
    public Spawner Spawner;

    [NonSerialized]
    public bool InLobby = false;

    // new
    [SerializeField]
    public NetworkPlayer LocalPlayer;

    [SerializeField]
    public List<NetworkPlayer> Players;

    public UnityEvent<NetworkPlayer> OnJoin;
    public UnityEvent<NetworkPlayer> OnLeave;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        Players = new List<NetworkPlayer>();
        
        Multiplayer = GetComponent<Multiplayer>();
        Spawner = GetComponent<Spawner>();

        Multiplayer.Connected.AddListener(Connected);
        Multiplayer.Disconnected.AddListener(Disconnected);
        Multiplayer.RoomJoined.AddListener(Join);
        Multiplayer.RoomLeft.AddListener(Leave);

        Multiplayer.RegisterRemoteProcedure("Internal_Message", Internal_Message);
        Multiplayer.RegisterRemoteProcedure("Internal_Join", Internal_Join);
        Multiplayer.RegisterRemoteProcedure("Internal_Leave", Internal_Leave);
    }

    //public LobbyPlayer Join()
    //{
    //    if (InLobby) 
    //    {
    //        Debug.LogWarning("Can't join! LobbyPlayer already in lobby");
    //        return null;
    //    }
    //    InLobby = true;
    //    GameObject gameObject = Spawner.Spawn(0);
    //    LobbyPlayer lobbyPlayer = gameObject.GetComponent<LobbyPlayer>();
    //    lobbyPlayer.Init(GenID, "Player " + GenID);
    //    LobbyPlayers.Add(lobbyPlayer);
    //    GenID++;

    //    Local = lobbyPlayer;

    //    ProcedureParameters parameters = new ProcedureParameters();
    //    parameters.Set("ID", lobbyPlayer.ID);

    //    MessageLocal(lobbyPlayer.Name + " joined");

    //    Multiplayer.InvokeRemoteProcedure("Internal_ConnectPlayer", UserId.All, parameters);
    //    OnConnected.Invoke(Multiplayer.Me, lobbyPlayer);
    //    return lobbyPlayer;
    //}

    //private void Internal_Join(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    //{
    //    ushort id = parameters.Get("ID", (ushort)0);
    //    GameObject gameObject = Spawner.Spawn(0);
    //    LobbyPlayer lobbyPlayer = gameObject.GetComponent<LobbyPlayer>();
    //    lobbyPlayer.Init(id, "Player " + id);
    //    LobbyPlayers.Add(lobbyPlayer);
    //    MessageLocal(lobbyPlayer.Name + " joined");
    //    //OnConnected.Invoke(Multiplayer.GetUser(fromUser), lobbyPlayer);
    //}

    //public void Leave()
    //{
    //    if (!InLobby)
    //    {
    //        Debug.LogWarning("Can't leave! LobbyPlayer not in a lobby");
    //        return;
    //    }
    //    InLobby = false;

    //    Spawner.Despawn(Local.gameObject);
    //    LobbyPlayers.Clear();
    //    ProcedureParameters parameters = new ProcedureParameters();
    //    parameters.Set("ID", Local.ID);

    //    MessageLocal(Local.Name + " left");

    //    Multiplayer.InvokeRemoteProcedure("Internal_DisconnectPlayer", UserId.All, parameters);
    //    OnDisconnected.Invoke(Multiplayer.Me, Local);
    //    Local = null;
    //}

    //private void Internal_Leave(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    //{
    //    ushort id = parameters.Get("ID", (ushort)0);
    //    LobbyPlayer leaving = null;
    //    foreach (LobbyPlayer player in LobbyPlayers)
    //    {
    //        if (id == player.ID)
    //        {
    //            leaving = player;
    //            LobbyPlayers.Remove(player);
    //            MessageLocal(leaving.Name + " left");
    //            OnDisconnected.Invoke(Multiplayer.GetUser(fromUser), leaving);
    //            return;
    //        }
    //    }
    //}

    public void Join(Multiplayer multiplayer, Room room, User user)
    {
        LocalPlayer.User = user;
        Players.Add(LocalPlayer);
        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("name", LocalPlayer.Name);
        parameters.Set("userId", LocalPlayer.User.Index);

        Multiplayer.InvokeRemoteProcedure("Internal_Join", UserId.All, parameters);
        OnJoin.Invoke(LocalPlayer);
        MessageLocal($"{LocalPlayer.Name} joined!");
    }

    private void Internal_Join(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        string name = parameters.Get("name", "player");
        ushort id = parameters.Get("userId", (ushort)0);
      
        NetworkPlayer player = new NetworkPlayer();
        player.User = Multiplayer.GetUser(id);
        player.Name = name;
        
        Players.Add(player);
        OnJoin.Invoke(player);
        MessageLocal($"{LocalPlayer.Name} joined!");
    }

    public void Leave(Multiplayer multiplayer)
    {
        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("name", LocalPlayer.Name);
        parameters.Set("userId", LocalPlayer.User.Index);

        Players.Clear();
        OnLeave.Invoke(LocalPlayer);
        MessageLocal($"{LocalPlayer.Name} left!");
    }

    private void Internal_Leave(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        string name = parameters.Get("name", "player");
        ushort id = parameters.Get("userId", (ushort)0);
        
        NetworkPlayer player = new NetworkPlayer();
        player.User = Multiplayer.GetUser(id);
        player.Name = name;

        foreach (var pl in Players)
        {
            if (player.User.Index == pl.User.Index)
            {
                Players.Remove(pl);
                MessageLocal($"{player.Name} left!");
                return;
            }
        }
    }
    public void Message(string message, bool prefix = true)
    {
        string msg = "";
        if (prefix)
        {
            msg = "Lobby: " + message;
        }
        else
        {
            msg = message;
        }
        Debug.Log(msg);
        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("msg", msg);
        Multiplayer.InvokeRemoteProcedure("Internal_Message", UserId.All, parameters);
    }

    public void MessageLocal(string message, bool prefix = true)
    {
        string msg = "";
        if (prefix)
        {
            msg = "Lobby: " + message;
        }
        else
        {
            msg = message;
        }
        Debug.Log(msg);
    }

    private void Internal_Message(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        string message = parameters.Get("msg", "default");
        Debug.Log(message);
    }

    // Events
    private void Connected(Multiplayer multiplayer, Endpoint endpoint)
    {
        Message("Connected to server");
        LocalPlayer = new NetworkPlayer();
        LocalPlayer.Name = "Player";

        Debug.Log($"Created new player: {LocalPlayer.Name}");
    }

    private void Disconnected(Multiplayer multiplayer, Endpoint endpoint) 
    {
        Message("Disconnected from server");
    }
}

[Serializable]
public class NetworkPlayer
{
    public string Name;
    public User User;
}