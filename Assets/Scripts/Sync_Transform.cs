using System;
using Alteruna;
using UnityEngine;

public class Sync_Transform : Synchronizable
{
    // Data to be synchronized with other players in our playroom.
    private Vector3 SynchronizedPosition;
    
    [SerializeField] private Transform SynchronizedTransform;
    
    // Used to store the previous version of our data so that we know when it has changed.
    private Vector3 _oldSynchronizedPosition;

    private void Start()
    {
        SynchronizedPosition = SynchronizedTransform.position;
    }
    
    void Update()
    {
        SynchronizedPosition = SynchronizedTransform.position;
        
        if (SynchronizedPosition != _oldSynchronizedPosition)
        {
            _oldSynchronizedPosition = SynchronizedPosition;
            Commit();
        }
        
        SyncUpdate();
    }

    public override void AssembleData(Writer writer, byte LOD = 100)
    {
        writer.Write(SynchronizedTransform.position);
    }

    public override void DisassembleData(Reader reader, byte LOD = 100)
    {
        SynchronizedPosition = reader.ReadVector3();
        SynchronizedTransform.position = SynchronizedPosition;
        _oldSynchronizedPosition = SynchronizedPosition;
    }
}
