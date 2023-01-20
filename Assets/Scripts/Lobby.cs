using Alteruna;
using Alteruna.Trinity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
 
public struct UserData
{
    bool IsAdmin;

}

[RequireComponent(typeof(Multiplayer), typeof(Spawner))]
public class Lobby : MonoBehaviour
{
    public static Lobby Instance;

    [NonSerialized]
    public Multiplayer Multiplayer;

    [NonSerialized]
    public Spawner Spawner;
    [SerializeField]
    public User[] PlayerUsers;
    [SerializeField]
    public bool IsPossessing = false;
    [SerializeField]
    public ushort PlayerID;

    public User Local;
    public User Admin;
    public List<User> Users;

    [Header("Events")]
    public UnityEvent<Multiplayer, User, string, bool> OnSendMessage;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        PlayerUsers = new User[4];

        Multiplayer = GetComponent<Multiplayer>();
        Spawner = GetComponent<Spawner>();

        Users = new List<User>();

        Multiplayer.Connected.AddListener(Connected);
        Multiplayer.Disconnected.AddListener(Disconnected);

        Multiplayer.RoomJoined.AddListener(JoinedRoom);
        Multiplayer.RoomLeft.AddListener(LeftRoom);
        Multiplayer.OtherUserJoined.AddListener(OtherJoinedRoom);
        Multiplayer.OtherUserLeft.AddListener(OtherLeftRoom);

        Multiplayer.RegisterRemoteProcedure("Internal_Message", Internal_Message);
        Multiplayer.RegisterRemoteProcedure("Internal_SetPlayerUser", Internal_SetPlayerUser);
        Multiplayer.RegisterRemoteProcedure("Internal_RemovePlayerUser", Internal_RemovePlayerUser);
        Multiplayer.RegisterRemoteProcedure("Internal_SetAdmin", Internal_SetAdmin);

        Multiplayer.RegisterRemoteProcedure("Internal_UpdateLobbyData", Internal_AddUser);
    }

    private void Connected(Multiplayer multiplayer, Endpoint endpoint)
    {
    }

    private void Disconnected(Multiplayer multiplayer, Endpoint endpoint)
    {
    }

    public void JoinedRoom(Multiplayer multiplayer, Room room, User user)
    {
        Local = user;

        if (user.Index == 0)
        {
            InitLobby(room, user);
        }

        MessageLobby(user, $"{user.Name} joined!");
    }

    public void OtherJoinedRoom(Multiplayer multiplayer, User user)
    {
        MessageLocal($"{user.Name} joined!");
        
        if (Local == Admin)
        {
            AddUser(user);
        }
    }

    public void LeftRoom(Multiplayer multiplayer)
    {

        for (ushort i = 0; i < PlayerUsers.Length; i++)
        {
            if (PlayerUsers[i] == null)
            {
                continue;
            }
            RemovePlayerUser(i, PlayerUsers[i]);
        }

        MessageLobby(Local, $"{Local.Name} left!");
    }

    public void OtherLeftRoom(Multiplayer multiplayer, User user)
    {
        MessageLocal($"{user.Name} left!");
    }

    public void AddUser(User user, ushort targetUser = (ushort)UserId.All)
    {
        if (Local != Admin)
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
        Multiplayer.InvokeRemoteProcedure("Internal_UpdateLobbyData", targetUser, parameters);
    }

    private void Internal_AddUser(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        Users.Clear();
        int count = parameters.Get("count", 0);
        for (int i = 0; i < count; i++)
        {
            string str = "user" + i.ToString();
            ushort id = parameters.Get(str, (ushort)0);
            Users.Add(Multiplayer.GetUser(id));
        }
        Debug.Log("Updated lobby data! " + count);
    }

    public void MessageLocal(string message)
    {
        string msg = "Lobby: " + message;
        Debug.Log(msg);
    }



    public void MessageLobby(User fromUser, string message, ushort targetUser = (ushort)UserId.All)
    {
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

    public void SetPlayerUser(ushort id, User user, ushort targetUser = (ushort)UserId.All)
    {
        if (IsPossessing)
        {
            foreach (User currUser in PlayerUsers)
            {
                if (currUser == null)
                {
                    continue;
                }
                if (Multiplayer.Me.Index == currUser.Index)
                {
                    RemovePlayerUser(PlayerID, Multiplayer.Me);
                    break;
                }
            }
        }

        IsPossessing = true;
        PlayerID = id;

        PlayerUsers[id] = user;
        MessageLocal($"{user.Name} possessed P{id + 1}!");
        //OnPlayerUserPossess.Invoke(id, user);

        Remote_SetPlayerUser(id, user, targetUser);
    }

    public void Remote_SetPlayerUser(ushort id, User user, ushort targetUser = (ushort)UserId.All)
    {
        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("id", id);
        parameters.Set("userId", user.Index);
        Multiplayer.InvokeRemoteProcedure("Internal_SetPlayerUser", targetUser, parameters);
    }

    private void Internal_SetPlayerUser(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        ushort id = parameters.Get("id", (ushort)0);
        ushort userId = parameters.Get("userId", (ushort)0);

        User user = Multiplayer.GetUser(userId);
        PlayerUsers[id] = user;
        MessageLocal($"{user.Name} possessed P{id + 1}!");
        //OnPlayerUserPossess.Invoke(id, user);
    }

    public void RemovePlayerUser(ushort id, User user, ushort targetUser = (ushort)UserId.All)
    {
        IsPossessing = false;
        PlayerUsers[id] = null;

        MessageLocal($"{user.Name} unpossessed P{id + 1}!");
        //OnPlayerUserUnpossess.Invoke(id, user);

        Remote_RemovePlayerUser(id, user, targetUser);
    }

    private void Remote_RemovePlayerUser(ushort id, User user, ushort targetUser = (ushort)UserId.All)
    {
        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("id", id);
        parameters.Set("userId", user.Index);
        Multiplayer.InvokeRemoteProcedure("Internal_RemovePlayerUser", targetUser, parameters);
    }

    private void Internal_RemovePlayerUser(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        ushort id = parameters.Get("id", (ushort)0);
        ushort userId = parameters.Get("userId", (ushort)0);
        User user = Multiplayer.GetUser(userId);

        PlayerUsers[id] = null;
        MessageLocal($"{user.Name} unpossessed P{id + 1}!");
        //OnPlayerUserUnpossess.Invoke(id, user);
    }

    public void SetAdmin(User user)
    {
        if (Admin != null) 
        {
            if (Admin == user)
            {
                return;
            }
        }
        Admin = user;
        MessageLocal($"{user.Name} is the new admin!");
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
        MessageLocal($"{user.Name} is the new admin!");
    }

    private void InitLobby(Room room, User user)
    {
        MessageLocal("Initialized lobby " + room.Name);
        SetAdmin(user);
        AddUser(user);
    }
}
