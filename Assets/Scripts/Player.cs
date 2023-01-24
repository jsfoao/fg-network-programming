using UnityEngine;
using Alteruna;
using System;

[RequireComponent(typeof(Alteruna.Avatar))]
public class Player : MonoBehaviour
{
    [SerializeField]
    public Alteruna.Avatar Avatar;

    private Lobby _lobby;

    public bool Enabled = false;

    private void Start()
    {
        _lobby = Lobby.Instance;
        Avatar = GetComponent<Alteruna.Avatar>();
        Avatar.OnPossessed.AddListener(Possessed);
        Avatar.OnUnpossessed.AddListener(Unpossessed);
    }

    public void Possessed(User user)
    {
        Avatar = GetComponent<Alteruna.Avatar>();
        Enabled = false;
    }


    public void Unpossessed(User user)
    {
        Avatar.Possessor = null;
    }
}