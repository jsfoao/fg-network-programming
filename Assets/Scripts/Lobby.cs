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

    [SerializeField]
    public User[] PlayerUsers;

    [SerializeField]
    public bool IsPossessing = false;

    [SerializeField]
    public ushort PlayerID;

    public UnityEvent<ushort, User> OnPlayerUserPossess;
    public UnityEvent<ushort, User> OnPlayerUserUnpossess;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        PlayerUsers = new User[4];

        Multiplayer = GetComponent<Multiplayer>();
        Spawner = GetComponent<Spawner>();

        Multiplayer.Connected.AddListener(Connected);
        Multiplayer.Disconnected.AddListener(Disconnected);

        Multiplayer.RoomJoined.AddListener(Join);
        Multiplayer.RoomLeft.AddListener(Leave);
        Multiplayer.OtherUserJoined.AddListener(OtherJoin);
        Multiplayer.OtherUserLeft.AddListener(OtherLeave);

        Multiplayer.RegisterRemoteProcedure("Internal_Message", Internal_Message);
        Multiplayer.RegisterRemoteProcedure("Internal_SetPlayerUser", Internal_SetPlayerUser);
        Multiplayer.RegisterRemoteProcedure("Internal_RemovePlayerUser", Internal_RemovePlayerUser);
    }

    public void Join(Multiplayer multiplayer, Room room, User user)
    {   
        MessageLocal($"You joined!");

    }

    public void OtherJoin(Multiplayer multiplayer, User user)
    {
        MessageLocal($"{user.Name} joined!");

        for (ushort i = 0; i < PlayerUsers.Length; i++)
        {
            if (PlayerUsers[i] == null)
            {
                continue;
            }
            Remote_SetPlayerUser(i, PlayerUsers[i], user);
        }

    }

    public void Leave(Multiplayer multiplayer)
    {
        MessageLocal($"You left!");

        if (IsPossessing)
        {
            RemovePlayerUser(PlayerID);
        }

        for (ushort i = 0; i < PlayerUsers.Length; i++)
        {
            if (PlayerUsers[i] == null)
            {
                continue;
            }

        }
    }

    public void OtherLeave(Multiplayer multiplayer, User user)
    {
        MessageLocal($"{user.Name} left!");
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
        MessageLocal("Connected to server");
    }

    private void Disconnected(Multiplayer multiplayer, Endpoint endpoint) 
    {
        MessageLocal("Disconnected from server");
    }

    public void SetPlayerUser(ushort id, User user, ushort targetUser = (ushort)UserId.AllInclusive)
    {
        if (targetUser == 0)
        {
            Local_SetPlayerUser(id);
            return;
        }
        if (targetUser == (ushort)UserId.AllInclusive) 
        {
            Local_SetPlayerUser(id);
            Remote_SetPlayerUser(id, user, (ushort)UserId.All);
            return;
        }
        else
        {
            Remote_SetPlayerUser(id, user, targetUser);
            return;
        }
    }

    private void Local_SetPlayerUser(ushort id)
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
                    RemovePlayerUser(PlayerID);
                    break;
                }
            }
        }

        IsPossessing = true;
        PlayerID = id;

        User user = Multiplayer.Me;
        PlayerUsers[id] = user;
        MessageLocal($"{user.Name} possessed P{id + 1}!");
        OnPlayerUserPossess.Invoke(id, Multiplayer.Me);
    }

    private void Remote_SetPlayerUser(ushort id, User user, ushort userId = (ushort)UserId.All)
    {
        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("id", id);
        parameters.Set("userId", user.Index);
        Multiplayer.InvokeRemoteProcedure("Internal_SetPlayerUser", userId, parameters);
    }

    private void Internal_SetPlayerUser(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        ushort id = parameters.Get("id", (ushort)0);
        ushort userId = parameters.Get("userId", (ushort)0);

        User user = Multiplayer.GetUser(userId);
        PlayerUsers[id] = user;
        MessageLocal($"{user.Name} possessed P{id + 1}!");
        OnPlayerUserPossess.Invoke(id, user);
    }

    public void RemovePlayerUser(ushort id)
    {
        IsPossessing = false;
        PlayerUsers[id] = null;

        MessageLocal($"{Multiplayer.Me.Name} unpossessed P{id + 1}!");
        OnPlayerUserUnpossess.Invoke(id, Multiplayer.Me);

        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("id", id);
        parameters.Set("userId", Multiplayer.Me.Index);
        Multiplayer.InvokeRemoteProcedure("Internal_RemovePlayerUser", UserId.All, parameters);
    }

    private void Internal_RemovePlayerUser(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
        ushort id = parameters.Get("id", (ushort)0);
        ushort userId = parameters.Get("userId", (ushort)0);
        User user = Multiplayer.GetUser(userId);

        PlayerUsers[id] = null;
        MessageLocal($"{user.Name} unpossessed P{id + 1}!");
        OnPlayerUserUnpossess.Invoke(id, user);
    }

    public void AvatarPossessed(User user)
    {
        MessageLocal("Avatar possessed");
    }
}
