using Alteruna;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class UserEntry
{
    public User User;
    public GameObject Go;

    public UserEntry(User user, GameObject go)
    {
        User = user;
        Go = go;
    }
}

[Serializable]
public class PlayerEntry
{
    public User User;
    public GameObject Go;

    public PlayerEntry(User user, GameObject go)
    {
        User = user;
        Go = go;
    }
}


public class LobbyMenu : MonoBehaviour
{
    private Lobby _lobby;
    [Header("Users")]
    [SerializeField]
    private GameObject _userEntryPrefab;
    [SerializeField]
    private GameObject _userEntryParent;

    [Header("Chat")]
    private ushort _maxChatEntries = 10;
    [SerializeField]
    private GameObject _chatEntryPrefab;
    [SerializeField]
    private GameObject _chatEntryParent;
    [SerializeField]
    private InputField _inputField;

    [Header("Match")]
    [SerializeField]
    private GameObject _startButton;
    [SerializeField]
    private List<PlayerEntry> _playerEntries;

    private List<UserEntry> _userEntries;
    private List<GameObject> _chatEntries;

    private void Start()
    {
        _lobby = Lobby.Instance;
        _userEntries = new List<UserEntry>();
        _chatEntries = new List<GameObject>();

        // User events
        _lobby.OnAddedUser.AddListener(RefreshUserEntries);
        _lobby.OnAddedUser.AddListener(delegate { ClearChat(); });
        _lobby.OnRemovedUser.AddListener(RemoveUserEntry);
        _lobby.OnSetAdmin.AddListener(SetAdmin);

        // Chat events
        _inputField.onSubmit.AddListener(SendChatMessage);
        _lobby.OnSendMessage.AddListener(CreateChatEntry);

        // Match events
        _lobby.OnPossessed.AddListener(PossessPanel);
        _lobby.OnUnpossessed.AddListener(UnpossessPanel);
        _lobby.OnStartMatch.AddListener(HideMenu);
        _lobby.OnEndMatch.AddListener(ShowMenu);
        _startButton.GetComponent<Button>().onClick.AddListener(delegate { _lobby.StartMatch(); });

        for (int i = 0; i < _playerEntries.Count; i++)
        {
            Text title = _playerEntries[i].Go.transform.Find("Title").gameObject.GetComponent<Text>();
            title.text = $"P{i + 1}";
            Button join = _playerEntries[i].Go.transform.Find("Join").gameObject.GetComponent<Button>();
            Button exit = _playerEntries[i].Go.transform.Find("Exit").gameObject.GetComponent<Button>();
            ushort id = (ushort)i;
            join.onClick.AddListener(delegate { _lobby.Possess(_lobby.Local, id); });
            exit.onClick.AddListener(delegate { _lobby.Unpossess(id); });
        }

    }

    public void SendChatMessage(string message)
    {
        if (message == "")
        {
            _inputField.ActivateInputField();
            return;
        }
        _lobby.MessageUser(Lobby.Instance.Local, message);
    }

    public void CreateChatEntry(Multiplayer multiplayer, User user, string message, bool lobby)
    {
        _inputField.text = "";
        _inputField.ActivateInputField();

        GameObject entryGo = Instantiate(_chatEntryPrefab, _chatEntryParent.transform);
        Text entryText = entryGo.GetComponentInChildren<Text>();
        entryText.text = message;

        if (lobby)
        {
            entryText.fontStyle = FontStyle.Italic;
        }
        else
        {
            entryText.fontStyle = FontStyle.Normal;
        }

        _chatEntries.Add(entryGo);

        if (_chatEntries.Count >= _maxChatEntries)
        {
            RemoveChatEntry(0);
        }
    }

    public void RemoveChatEntry(int i)
    {
        Destroy(_chatEntries[i]);
        _chatEntries.RemoveAt(i);
    }

    public void ClearChat()
    {
        foreach (GameObject entry in _chatEntries)
        {
            Destroy(entry);
        }
        _chatEntries.Clear();
    }

    public void RefreshUserEntries(Multiplayer multiplayer, User user)
    {
        RemoveAllUserEntries();
        foreach (User us in _lobby.Users)
        {
            CreateUserEntry(multiplayer, us);
        }
        SetAdmin(multiplayer, _lobby.Admin);
    }

    public UserEntry CreateUserEntry(Multiplayer multiplayer, User user)
    {
        GameObject go = Instantiate(_userEntryPrefab, _userEntryParent.transform);
        Text pText = go.transform.Find("Name").GetComponent<Text>();
        Text pData = go.transform.Find("Data").GetComponent<Text>();

        pText.text = user.Name;
        if (user == multiplayer.Me)
        {
            pText.fontStyle = FontStyle.Bold;
        }
        else
        {
            pText.fontStyle = FontStyle.Normal;
        }

        UserEntry entry = new UserEntry(user, go);
        _userEntries.Add(entry);

        if (user == _lobby.Admin)
        {
            SetAdmin(multiplayer, user);
        }
        return entry;
    }

    public void RemoveAllUserEntries()
    {
        foreach (var entry in _userEntries)
        {
            Destroy(entry.Go);
        }
        _userEntries.Clear();
    }

    public void RemoveUserEntry(Multiplayer multiplayer, User user)
    {
        if (_lobby.Local == user)
        {
            RemoveAllUserEntries();
            return;
        }
        foreach (var entry in _userEntries)
        {
            if (entry.User.Index == user.Index)
            {
                Destroy(entry.Go);
                _userEntries.Remove(entry);
                return;
            }
        }
    }

    private GameObject GetEntry(User user)
    {
        foreach (var entry in _userEntries)
        {
            if (entry.User == user)
            {
                return entry.Go;
            }
        }
        return null;
    }

    public void PossessPanel(User user, ushort id)
    {
        _playerEntries[id].User = user;
        GameObject joinButton = _playerEntries[id].Go.transform.Find("Join").gameObject;
        joinButton.SetActive(false);

        GameObject exitButton = _playerEntries[id].Go.transform.Find("Exit").gameObject;
        exitButton.SetActive(true);

        if (user != _lobby.Local)
        {
            exitButton.GetComponent<Button>().interactable = false;
        }

        Text title = _playerEntries[id].Go.transform.Find("Title").GetComponent<Text>();
        title.text = user.Name;
    }

    public void UnpossessPanel(User user, ushort id)
    {
        _playerEntries[id].User = null;
        GameObject joinButton = _playerEntries[id].Go.transform.Find("Join").gameObject;
        joinButton.SetActive(true);

        GameObject exitButton = _playerEntries[id].Go.transform.Find("Exit").gameObject;
        exitButton.SetActive(false);

        exitButton.GetComponent<Button>().interactable = true;

        Text title = _playerEntries[id].Go.transform.Find("Title").GetComponent<Text>();
        title.text = $"P{id+1}";
    }

    public void SetAdmin(Multiplayer multiplayer, User user)
    {
        foreach (var entry in _userEntries)
        {
            Text pData = entry.Go.transform.Find("Data").GetComponent<Text>();
            if (entry.User == user)
            {
                pData.text = "(H)";
            }
            else
            {
                pData.text = "";
            }
        }

        if (_lobby.Local == user)
        {
            _startButton.GetComponent<Button>().interactable = true;
        }
        else
        {
            _startButton.GetComponent<Button>().interactable = false;
        }
    }

    private void HideMenu()
    {
        gameObject.SetActive(false);
    }

    private void ShowMenu()
    {
        gameObject.SetActive(true);
    }
}
