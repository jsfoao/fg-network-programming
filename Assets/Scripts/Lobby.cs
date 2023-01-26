using Alteruna;
using Alteruna.Trinity;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class PlayerData
{
    public User User;
    public Transform SpawnLocation;
    public Walls Goal;
}

[RequireComponent(typeof(Multiplayer), typeof(Spawner))]
public class Lobby : MonoBehaviour
{
    public static Lobby Instance;

    [NonSerialized]
    public Multiplayer Multiplayer;

    [NonSerialized]
    public Spawner Spawner;

    [Header("Spawning")]
    [SerializeField]
    public Transform CornerSpawn;
    [SerializeField]
    public List<PlayerData> PlayersData;

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
    public UnityEvent<User, ushort> OnPossessed;
    public UnityEvent<User, ushort> OnUnpossessed;

    // Match
    public UnityEvent OnStartMatch;
    public UnityEvent OnEndMatch;

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
        Multiplayer.RegisterRemoteProcedure("Internal_Possess", Internal_Possess);
        Multiplayer.RegisterRemoteProcedure("Internal_Unpossess", Internal_Unpossess);
        Multiplayer.RegisterRemoteProcedure("Internal_StartMatch", Internal_StartMatch);
        Multiplayer.RegisterRemoteProcedure("Internal_EndMatch", Internal_EndMatch);
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

        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("newUser", user.Index);
        parameters.Set("adminId", Admin.Index);
        parameters.Set("count", Users.Count);
        // Send list of users
        for (int i = 0; i < Users.Count; i++)
        {
            string str = "user" + i.ToString();
            parameters.Set(str, Users[i].Index);
        }
        // Send list of possessed players
        for (int i = 0; i < PlayersData.Count; i++)
        {
            string str = "data" + i.ToString();
            if (PlayersData[i].User != null)
            {
                parameters.Set(str, PlayersData[i].User.Index);
            }
            else
            {
                parameters.Set(str, ushort.MaxValue);
            }
        }

        OnAddedUser.Invoke(Multiplayer, user);
        Multiplayer.InvokeRemoteProcedure("Internal_AddUser", targetUser, parameters);
    }

    private void Internal_AddUser(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        Users.Clear();
        User newUser = Multiplayer.GetUser(parameters.Get("newUser", (ushort)0));
        User admin = Multiplayer.GetUser(parameters.Get("adminId", (ushort)0));
        int count = parameters.Get("count", 0);
        for (int i = 0; i < count; i++)
        {
            string str = "user" + i.ToString();
            ushort id = parameters.Get(str, (ushort)0);
            Users.Add(Multiplayer.GetUser(id));
        }
        for (int i = 0; i < PlayersData.Count; i++)
        {
            string str = "data" + i.ToString();
            ushort id = parameters.Get(str, (ushort)0);
            if (id != ushort.MaxValue)
            {
                ushort playerId = (ushort)i;
                Possess(Multiplayer.GetUser(id), playerId);
            }
        }
        Admin = admin;
        OnAddedUser.Invoke(Multiplayer, newUser);

        Debug.Log($"Admin is {Admin.Name}");
    }

    public void RemoveUser(User user, ushort targetUser = (ushort)UserId.All)
    {
        if (!IsAdmin()) 
        {
            return;
        }
        Users.Remove(user);

        for (int i = 0; i < PlayersData.Count; i++)
        {
            if (PlayersData[i].User == user)
            {
                ushort id = (ushort)i;
                Unpossess(id);
            }
        }

        if (user == Admin && Users.Count > 0)
        {
            SetAdmin(Users[0]);
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

    public void Possess(User user, ushort id)
    {
        if (PlayersData[id].User != null)
        {
            Unpossess(id);
        }
        for (int i = 0; i < PlayersData.Count; i++)
        {
            if (user == PlayersData[i].User)
            {
                ushort currId = (ushort)i;
                Unpossess(currId);
            }
        }

        PlayersData[id].User = user;
        Player player = GetPlayer(user);
        player.transform.position = PlayersData[id].SpawnLocation.position;
        player.transform.rotation = PlayersData[id].SpawnLocation.rotation;

        //MessageLobby($"{Local.Name} possessed player {id+1}");
        OnPossessed.Invoke(user, id);

        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("userId", user.Index);
        parameters.Set("id", id);
        Multiplayer.InvokeRemoteProcedure("Internal_Possess", UserId.All, parameters);
    }

    private void Internal_Possess(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        User user = Multiplayer.GetUser(parameters.Get("userId", (ushort)(0)));
        ushort id = parameters.Get("id", (ushort)(0));
        PlayersData[id].User = user;

        //MessageLobby($"{user.Name} possessed player {id}");
        OnPossessed.Invoke(user, id);
    }

    public void Unpossess(ushort id)
    {
        User user = PlayersData[id].User;
        PlayersData[id].User = null;

        Player player = GetPlayer(user);
        player.transform.position = CornerSpawn.position;
        player.transform.rotation = CornerSpawn.rotation;

        //MessageLobby($"{user.Name} unpossessed player {id+1}");
        OnUnpossessed.Invoke(user, id);

        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("userId", user.Index);
        parameters.Set("id", id);

        Multiplayer.InvokeRemoteProcedure("Internal_Unpossess", UserId.All, parameters);
    }

    private void Internal_Unpossess(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        User user = Multiplayer.GetUser(parameters.Get("userId", (ushort)(0)));
        ushort id = parameters.Get("id", (ushort)(0));

        PlayersData[id].User = null;

        //MessageLobby($"{user.Name} unpossessed player {id}");
        OnUnpossessed.Invoke(user, id);
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

    public Player GetPlayer(User user)
    {
        Player[] players = FindObjectsOfType<Player>();
        foreach (var player in players)
        {
            if (player.Avatar.Possessor == user)
            {
                return player;
            }
        }
        return null;
    }

    public void StartMatch(ushort targetUser = (ushort)UserId.All)
    {
        if (!IsAdmin())
        {
            return;
        }

        for (int i = 0; i < PlayersData.Count; i++)
        {
            User user = PlayersData[i].User;
            if (user == null)
            {
                MessageLobby("Can't start. Not all players are possessed!");
                return;
            }
            Player player = GetPlayer(user);
            player.Enabled = true;
        }

        MessageLobby("Starting match...");
        OnStartMatch.Invoke();

        ProcedureParameters parameters = new ProcedureParameters();
        Multiplayer.InvokeRemoteProcedure("Internal_StartMatch", targetUser, parameters);
    }

    private void Internal_StartMatch(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        for (int i = 0; i < PlayersData.Count; i++)
        {
            Player player = GetPlayer(PlayersData[i].User);
            player.Enabled = true;
        }

        OnStartMatch.Invoke();
    }

    public void EndMatch(ushort targetUser = (ushort)UserId.All)
    {
        for (int i = 0; i < PlayersData.Count; i++)
        {
            Player player = GetPlayer(PlayersData[i].User);
            if (player == null)
            {
                continue;
            }
            player.Enabled = false;
        }
        MessageLobby("Ending match...");
        OnEndMatch.Invoke();

        ProcedureParameters parameters = new ProcedureParameters();
        Multiplayer.InvokeRemoteProcedure("Internal_EndMatch", targetUser, parameters);
    }

    private void Internal_EndMatch(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        for (int i = 0; i < PlayersData.Count; i++)
        {
            Player player = GetPlayer(PlayersData[i].User);
            if (player == null)
            {
                continue;
            }
            player.Enabled = false;
        }

        OnEndMatch.Invoke();
    }
}
