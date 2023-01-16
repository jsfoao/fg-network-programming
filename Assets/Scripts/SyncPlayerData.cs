using Alteruna;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SyncPlayerData : MonoBehaviour
{
    [NonSerialized]
    public List<SyncAttribute> Attributes;
    private PlayerData _playerData;

    private void Start()
    {
        Attributes = new List<SyncAttribute>();
        _playerData = GetComponent<PlayerData>();
        Attributes.Add(new SyncAttribute(_playerData.Health));
    }

    private void Update()
    {
        foreach (SyncAttribute attr in Attributes) 
        {
            attr.Sync();
            Debug.Log("Synced Health: " + attr.GetValue());
        }
    }
}

public class SyncAttribute : Synchronizable
{
    private Attribute _attr;
    private int _oldSyncedValue;

    public SyncAttribute(Attribute attr)
    {
        Bind(attr);
    }

    public override void DisassembleData(Reader reader, byte LOD = 100)
    {
        _attr.Value = reader.ReadInt();
        _oldSyncedValue = _attr.Value;
    }

    public override void AssembleData(Writer writer, byte LOD = 100)
    {
        writer.Write(_attr.Value);
    }

    public void Bind(Attribute attr)
    {
        _attr = attr;
    }

    public int GetValue() 
    {
        return _attr.Value;
    }

    // Synchronizes binded attribute
    public void Sync()
    {
        if (_attr.Value != _oldSyncedValue)
        {
            _oldSyncedValue = _attr.Value;
            Commit();
        }
        base.SyncUpdate();
    }
}
