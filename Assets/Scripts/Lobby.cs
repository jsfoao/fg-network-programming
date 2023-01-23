using Alteruna;
using Alteruna.Trinity;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    [SerializeField]
    public List<LobbyPlayer> Players;

    // Ourselves, local user
    public User Local;
    // Admin of lobby
    public User Admin;
    // All users connected to lobby, ordered from arrival
    public List<User> Users;

    [Header("Events")]
    public UnityEvent<Multiplayer, User, string, bool> OnSendMessage;
    public UnityEvent<Multiplayer, User> OnAddedUser;
    public UnityEvent<Multiplayer, User> OnRemovedUser;
    public UnityEvent<Multiplayer, User> OnSetAdmin;
    public UnityEvent<User, ushort> OnPossessedPlayer;
    public UnityEvent<User, ushort> OnUnpossessedPlayer;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        Multiplayer = GetComponent<Multiplayer>();
        Spawner = GetComponent<Spawner>();

        Users = new List<User>();

        Multiplayer.RoomJoined.AddListener(JoinedRoom);
        Multiplayer.RoomLeft.AddListener(LeftRoom);
        Multiplayer.OtherUserJoined.AddListener(OtherJoinedRoom);
        Multiplayer.OtherUserLeft.AddListener(OtherLeftRoom);

        Multiplayer.RegisterRemoteProcedure("Internal_Message", Internal_Message);

        Multiplayer.RegisterRemoteProcedure("Internal_SetAdmin", Internal_SetAdmin);
        Multiplayer.RegisterRemoteProcedure("Internal_AddUser", Internal_AddUser);
        Multiplayer.RegisterRemoteProcedure("Internal_RemoveUser", Internal_RemoveUser);
        Multiplayer.RegisterRemoteProcedure("Internal_PossessPlayer", Internal_PossessPlayer);
        Multiplayer.RegisterRemoteProcedure("Internal_UnpossessPlayer", Internal_UnpossessPlayer);
    }

    public void JoinedRoom(Multiplayer multiplayer, Room room, User user)
    {
        Local = user;

        if (user.Index == 0)
        {
            InitLobby(room, user);
        }

        MessageLobby($"{user.Name} joined!");
    }

    public void OtherJoinedRoom(Multiplayer multiplayer, User user)
    {
        AddUser(user);
        MessageLobby($"{user.Name} joined!");
    }

    public void LeftRoom(Multiplayer multiplayer)
    {

        //for (ushort i = 0; i < PlayerUsers.Length; i++)
        //{
        //    if (PlayerUsers[i] == null)
        //    {
        //        continue;
        //    }
        //    RemovePlayerUser(i, PlayerUsers[i]);
        //}
        
        RemoveUser(Local);
        MessageLobby($"{Local.Name} left!");
    }

    public void OtherLeftRoom(Multiplayer multiplayer, User user)
    {
        MessageLocal($"{user.Name} left!");
    }

    public void AddUser(User user, ushort targetUser = (ushort)UserId.All)
    {
        if (!IsAdmin())
        {
            return;
        }
        // Add new user
        Users.Add(user);

        // Send list of all users to new user
        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("count", Users.Count);
        for (int i = 0; i < Users.Count; i++)
        {
            string str = "user" + i.ToString();
            parameters.Set(str, Users[i].Index);
        }
        parameters.Set("newUser", user.Index);

        OnAddedUser.Invoke(Multiplayer, user);
        Multiplayer.InvokeRemoteProcedure("Internal_AddUser", targetUser, parameters);
    }

    private void Internal_AddUser(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        Users.Clear();
        User newUser = Multiplayer.GetUser(parameters.Get("newUser", (ushort)0));
        int count = parameters.Get("count", 0);
        for (int i = 0; i < count; i++)
        {
            string str = "user" + i.ToString();
            ushort id = parameters.Get(str, (ushort)0);
            Users.Add(Multiplayer.GetUser(id));
        }
        OnAddedUser.Invoke(Multiplayer, newUser);
    }

    public void RemoveUser(User user, ushort targetUser = (ushort)UserId.All)
    {
        if (!IsAdmin()) 
        {
            return;
        }
        Users.Remove(user);

        if (user == Admin && Users.Count > 0)
        {
            SetAdmin(Users[0]);
        }

        foreach (var player in Players)
        {
            if (player.Owner == user)
            {
                player.Unpossess();
            }
        }
        OnRemovedUser.Invoke(Multiplayer, user);

        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("removedUser", user.Index);
        Multiplayer.InvokeRemoteProcedure("Internal_RemoveUser", targetUser, parameters);
    }

    private void Internal_RemoveUser(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        User removedUser = Multiplayer.GetUser(parameters.Get("removedUser", (ushort)0));
        Users.Remove(removedUser);
        OnRemovedUser.Invoke(Multiplayer, removedUser);
    }

    public void MessageLocal(string message)
    {
        string msg = "Lobby: " + message;
        Debug.Log(msg);
    }

    public void MessageLobby(string message, ushort targetUser = (ushort)UserId.All)
    {
        if (!IsAdmin())
        {
            return;
        }
        string prefix = "Lobby";
        string entry = $"{prefix}: {message}";
        Debug.Log(entry);
        OnSendMessage.Invoke(Multiplayer, Local, message, true);
        Remote_Message(Local, prefix, message);
    }

    public void MessageUser(User fromUser, string message, ushort targetUser = (ushort)UserId.All)
    {
        string entry = $"You: {message}";
        Debug.Log(entry);
        OnSendMessage.Invoke(Multiplayer, fromUser, entry, false);
        Remote_Message(fromUser, fromUser.Name, message);
    }

    public void Remote_Message(User user, string prefix, string message, ushort targetUser = (ushort)UserId.All)
    {
        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("prefix", prefix);
        parameters.Set("msg", message);
        parameters.Set("id", user.Index);
        Multiplayer.InvokeRemoteProcedure("Internal_Message", UserId.All, parameters);
    }

    private void Internal_Message(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        string prefix = parameters.Get("prefix", "default");
        string message = parameters.Get("msg", "default");
        ushort id = parameters.Get("id", (ushort)0);
        User user = Multiplayer.GetUser(id);
        bool lobby = prefix == "Lobby";
        string entry = $"{prefix}: {message}";
        Debug.Log(entry);
        OnSendMessage.Invoke(Multiplayer, user, entry, lobby);
    }

    public void PossessPlayer(User user, ushort playerId)
    {
        foreach (var player in Players)
        {
            if (player.ID == playerId)
            {
                player.Possess(user);
                break;
            }
        }

        OnPossessedPlayer.Invoke(user, playerId);
        MessageLobby($"{user.Name} possessed player {playerId}");

        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("userId", user.Index);
        parameters.Set("playerId", playerId);

        Multiplayer.InvokeRemoteProcedure("Internal_PossessPlayer", UserId.All, parameters);
    }

    private void Internal_PossessPlayer(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        User user = Multiplayer.GetUser(parameters.Get("userId", (ushort)(0)));
        ushort playerId = parameters.Get("playerId", (ushort)(0));

        foreach (var player in Players)
        {
            if (player.ID == playerId)
            {
                player.Possess(user);
                break;
            }
        }
        MessageLobby($"{user.Name} possessed player {playerId}");
        OnPossessedPlayer.Invoke(user, playerId);
    }

    public void UnpossessPlayer(ushort playerId)
    {
        LobbyPlayer player = null;
        User owner = null;
        foreach (var pl in Players)
        {
            if (pl.ID == playerId)
            {
                player = pl;
                owner = player.Owner;
                break;
            }
        }

        MessageLobby($"{player.Owner.Name} unpossessed player {playerId}");
        player.Unpossess();
        OnUnpossessedPlayer.Invoke(owner, playerId);

        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("userId", owner.Index);
        parameters.Set("playerId", playerId);

        Multiplayer.InvokeRemoteProcedure("Internal_UnpossessPlayer", UserId.All, parameters);
    }

    private void Internal_UnpossessPlayer(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        User user = Multiplayer.GetUser(parameters.Get("userId", (ushort)(0)));
        ushort playerId = parameters.Get("playerId", (ushort)(0));

        foreach (var pl in Players)
        {
            if (pl.ID == playerId)
            {
                pl.Unpossess();
                break;
            }
        }

        MessageLobby($"{user.Name} unpossessed player {playerId}");
        OnUnpossessedPlayer.Invoke(user, playerId);
    }

    public void SetAdmin(User user)
    {
        if (Admin == user)
        {
            return;
        }
        Admin = user;
        MessageLobby($"{user.Name} is the new admin!");

        OnSetAdmin.Invoke(Multiplayer, user);
        Remote_SetAdmin(user);
    }

    public void Remote_SetAdmin(User user, ushort targetUser = (ushort)UserId.All)
    {
        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("adminId", user.Index);
        Multiplayer.InvokeRemoteProcedure("Internal_SetAdmin", targetUser, parameters);
    }

    private void Internal_SetAdmin(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        ushort id = parameters.Get("adminId", (ushort)0);
        User user = Multiplayer.GetUser(id);
        Admin = user;

        OnSetAdmin.Invoke(Multiplayer, user);
    }

    private void InitLobby(Room room, User user)
    {
        SetAdmin(user);
        AddUser(user);
    }

    public bool IsAdmin()
    {
        return Local == Admin;
    }
}
