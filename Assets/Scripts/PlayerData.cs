using System;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    [SerializeField]
    protected PlayerDefaultData DefaultData;

    [NonSerialized]
    public Attribute Health;

    private void Start()
    {
        Health = new Attribute(DefaultData.Health);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            Health.Value += 1;
        }
    }
}

[Serializable]
public class Attribute
{
    public int Value;

    public Attribute()
    {
        Value = 0;
    }

    public Attribute(int value)
    {
        Value = value;
    }
}