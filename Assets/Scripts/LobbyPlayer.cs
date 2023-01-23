using UnityEngine;
using Alteruna;
using System;

[RequireComponent(typeof(Alteruna.Avatar))]
public class LobbyPlayer : MonoBehaviour
{
    [NonSerialized]
    public Alteruna.Avatar Avatar;

    [SerializeField]
    public ushort ID;

    [SerializeField ]
    public User Owner;

    private void Start()
    {
        Avatar = GetComponent<Alteruna.Avatar>();
    }

    public void Possess(User user)
    {
        if (Owner != null)
        {
            Unpossess();
        }
        Owner = user;
        Avatar.Possessor = Owner;
        Avatar.OnPossessed.Invoke(user);
    }

    public void Unpossess()
    {
        if (Owner == null) 
        {
            return;
        }
        Avatar.OnUnpossessed.Invoke(Owner);
        Owner = null;
        Avatar.Possessor = null;
    }
}
