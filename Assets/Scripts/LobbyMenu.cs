using Alteruna;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _entryPrefab;

    [SerializeField]
    private GameObject _entriesObject;

    private List<Tuple<ushort, GameObject>> _entries;

    private void Start()
    {
        _entries = new List<Tuple<ushort, GameObject>>();
        Lobby.Instance.OnJoin.AddListener(AddPlayerEntry);
        Lobby.Instance.OnLeave.AddListener(RemovePlayerEntry);
    }
    public void AddPlayerEntry(NetworkPlayer player)
    {
        GameObject entryGo = Instantiate(_entryPrefab, _entriesObject.transform);
        Text pName = _entryPrefab.transform.Find("Name").GetComponent<Text>();
        pName.text = player.Name;

        Debug.Log("Entry: " + player.Name);
        Tuple<ushort, GameObject> entry = new Tuple<ushort, GameObject>(player.User.Index, entryGo);
        _entries.Add(entry);
    }

    public void RemovePlayerEntry(NetworkPlayer player)
    {
        foreach (var entry in _entries)
        {
            if (entry.Item1 == player.User.Index)
            {
                Destroy(entry.Item2);
                _entries.Remove(entry);
                return;
            }
        }
    }

}
