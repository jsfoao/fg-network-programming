using Alteruna;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class LobbyMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _entryPrefab;

    [SerializeField]
    private GameObject _entriesObject;

    private List<Tuple<User, GameObject>> _entries;

    [SerializeField]
    private List<GameObject> _playerPanels;

    private void Start()
    {
        _entries = new List<Tuple<User, GameObject>>();
        Lobby.Instance.Multiplayer.RoomJoined.AddListener(CreateAllUserEntries);
        Lobby.Instance.Multiplayer.OtherUserJoined.AddListener(CreateUserEntry);
        Lobby.Instance.Multiplayer.RoomLeft.AddListener(RemoveAllUserEntries);
        Lobby.Instance.Multiplayer.OtherUserLeft.AddListener(RemoveUserEntry);

        Lobby.Instance.OnPlayerUserPossess.AddListener(PossessPanel);
        Lobby.Instance.OnPlayerUserUnpossess.AddListener(UnpossessPanel);

        for (ushort i = 0; i < _playerPanels.Count; i++)
        {
            UnpossessPanel(i, null);
        }
    }
    public void CreateAllUserEntries(Multiplayer multiplayer, Room room, User user)
    {
        foreach (User roomUser in room.Users)
        {
            CreateUserEntry(multiplayer, roomUser);
        }
    }

    public void CreateUserEntry(Multiplayer multiplayer, User user)
    {
        GameObject entryGo = Instantiate(_entryPrefab, _entriesObject.transform);
        Text pText = entryGo.transform.Find("Name").GetComponent<Text>();
        Text pData = entryGo.transform.Find("Data").GetComponent<Text>();

        pText.text = user.Name;
        if (user == multiplayer.Me)
        {
            pText.fontStyle = FontStyle.Bold;
        }
        else
        {
            pText.fontStyle = FontStyle.Normal;
        }

        Tuple<User, GameObject> entry = new Tuple<User, GameObject>(user, entryGo);
        _entries.Add(entry);
    }

    public void RemoveAllUserEntries(Multiplayer multiplayer)
    {
        foreach (var entry in _entries)
        {
            Destroy(entry.Item2);
        }
        _entries.Clear();
    }

    public void RemoveUserEntry(Multiplayer multiplayer, User user)
    {
        foreach (var entry in _entries)
        {
            if (entry.Item1.Index == user.Index)
            {
                Destroy(entry.Item2);
                _entries.Remove(entry);
                return;
            }
        }
    }

    private GameObject GetEntry(User user)
    {
        foreach (var entry in _entries)
        {
            if (entry.Item1 == user)
            {
                return entry.Item2;
            }
        }
        return null;
    }

    public void UnpossessPanel(ushort id, User user)
    {
        GameObject buttonGo = _playerPanels[id].transform.Find("Button").gameObject;
        buttonGo.GetComponent<Button>().onClick.RemoveAllListeners();
        buttonGo.GetComponent<Button>().onClick.AddListener(delegate { Lobby.Instance.SetPlayerUser(id); });
       
        Text playerText = _playerPanels[id].transform.Find("Title").GetComponent<Text>();
        playerText.text = $"P{id + 1}";
        Text buttonText = buttonGo.transform.Find("Text").GetComponent<Text>();
        buttonText.text = "Join";

        if (user == null) 
        {
            return;
        }

        GameObject entryGo = GetEntry(user);
        Text pData = entryGo.transform.Find("Data").GetComponent<Text>();
        pData.text = "";
    }

    public void PossessPanel(ushort id, User user)
    {
        GameObject buttonGo = _playerPanels[id].transform.Find("Button").gameObject;
        
        buttonGo.GetComponent<Button>().onClick.RemoveAllListeners();
        buttonGo.GetComponent<Button>().onClick.AddListener(delegate { Lobby.Instance.RemovePlayerUser(id); });

        Text buttonText = buttonGo.transform.Find("Text").GetComponent<Text>();
        buttonText.text = "Exit";

        Text playerText = _playerPanels[id].transform.Find("Title").GetComponent<Text>();
        playerText.text = user.Name;

        GameObject entryGo = GetEntry(user);
        Text pData = entryGo.transform.Find("Data").GetComponent<Text>();
        pData.text = "P" + (id + 1).ToString();
    }
}
