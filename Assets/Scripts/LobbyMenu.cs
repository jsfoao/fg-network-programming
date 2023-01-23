using Alteruna;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMenu : MonoBehaviour
{
    private Lobby _lobby;
    [Header("Users")]
    [SerializeField]
    private GameObject _userEntryPrefab;
    [SerializeField]
    private GameObject _userEntryParent;

    [Header("Chat")]
    [SerializeField]
    private GameObject _chatEntryPrefab;
    [SerializeField]
    private GameObject _chatEntryParent;

    private List<Tuple<User, GameObject>> _userEntries;

    [SerializeField]
    private List<GameObject> _playerPanels;

    private void Start()
    {
        _lobby = Lobby.Instance;
        _userEntries = new List<Tuple<User, GameObject>>();
        _lobby.OnAddedUser.AddListener(RefreshUserEntries);
        _lobby.OnRemovedUser.AddListener(RemoveUserEntry);

        transform.Find("Chat").transform.Find("Input").GetComponent<InputField>().onSubmit.AddListener(SendChatMessage);
        _lobby.OnSendMessage.AddListener(CreateChatEntry);
        Lobby.Instance.OnPossessedPlayer.AddListener(PossessPanel);
        Lobby.Instance.OnUnpossessedPlayer.AddListener(UnpossessPanel);

        for (ushort i = 0; i < _playerPanels.Count; i++)
        {
            UnpossessPanel(null, i);
        }
    }

    public void SendChatMessage(string message)
    {
        if (message == "")
        {
            transform.Find("Chat").transform.Find("Input").GetComponent<InputField>().ActivateInputField();
            return;
        }
        _lobby.MessageUser(Lobby.Instance.Local, message);
    }

    public void CreateChatEntry(Multiplayer multiplayer, User user, string message, bool lobby)
    {
        transform.Find("Chat").transform.Find("Input").GetComponent<InputField>().text = "";
        transform.Find("Chat").transform.Find("Input").GetComponent<InputField>().ActivateInputField();

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
    }

    public void RefreshUserEntries(Multiplayer multiplayer, User user)
    {
        RemoveAllUserEntries();
        foreach (User us in _lobby.Users)
        {
            CreateUserEntry(multiplayer, us);
        }
    }

    public void CreateUserEntry(Multiplayer multiplayer, User user)
    {
        GameObject entryGo = Instantiate(_userEntryPrefab, _userEntryParent.transform);
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
        _userEntries.Add(entry);
    }

    public void RemoveAllUserEntries()
    {
        foreach (var entry in _userEntries)
        {
            Destroy(entry.Item2);
        }
        _userEntries.Clear();
    }

    public void RemoveUserEntry(Multiplayer multiplayer, User user)
    {
        foreach (var entry in _userEntries)
        {
            if (entry.Item1.Index == user.Index)
            {
                Destroy(entry.Item2);
                _userEntries.Remove(entry);
                return;
            }
        }
    }

    private GameObject GetEntry(User user)
    {
        foreach (var entry in _userEntries)
        {
            if (entry.Item1 == user)
            {
                return entry.Item2;
            }
        }
        return null;
    }

    public void UnpossessPanel(User user, ushort id)
    {
        GameObject buttonGo = _playerPanels[id].transform.Find("Button").gameObject;
        buttonGo.GetComponent<Button>().onClick.RemoveAllListeners();
        int playerId = id - 1;
        buttonGo.GetComponent<Button>().onClick.AddListener(delegate { _lobby.PossessPlayer(Lobby.Instance.Local, (ushort)playerId); });
       
        Text playerText = _playerPanels[id].transform.Find("Title").GetComponent<Text>();
        playerText.text = $"P{id + 1}";
        Text buttonText = buttonGo.transform.Find("Text").GetComponent<Text>();
        buttonText.text = "Join";

        buttonGo.GetComponent<Button>().interactable = true;
        if (user == null) 
        {
            return;
        }

        GameObject entryGo = GetEntry(user);
        Text pData = entryGo.transform.Find("Data").GetComponent<Text>();
        pData.text = "";
    }

    public void PossessPanel(User user, ushort id)
    {
        GameObject buttonGo = _playerPanels[id].transform.Find("Button").gameObject;
        
        buttonGo.GetComponent<Button>().onClick.RemoveAllListeners();
        int playerId = id + 1;
        buttonGo.GetComponent<Button>().onClick.AddListener(delegate { _lobby.UnpossessPlayer(Lobby.Instance.Local, (ushort)playerId); });

        Button button = buttonGo.GetComponent<Button>();
        Text buttonText = buttonGo.transform.Find("Text").GetComponent<Text>();
        buttonText.text = "Exit";

        if (user.Index != Lobby.Instance.Multiplayer.Me.Index)
        {
            buttonText.text = "-";
            buttonGo.GetComponent<Button>().interactable = false;
        }

        Text playerText = _playerPanels[id].transform.Find("Title").GetComponent<Text>();
        playerText.text = user.Name;

        GameObject entryGo = GetEntry(user);
        Text pData = entryGo.transform.Find("Data").GetComponent<Text>();
        pData.text = "P" + (id + 1).ToString();
    }
}
