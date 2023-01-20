using UnityEngine;
using Alteruna;
using System;

[RequireComponent(typeof(Alteruna.Avatar))]
public class LobbyPlayer : MonoBehaviour
{
    [NonSerialized]
    public Alteruna.Avatar Avatar;

    [SerializeField ]
    public ushort AvatarID;

    private void Start()
    {
        Avatar = GetComponent<Alteruna.Avatar>();
        Avatar.OnPossessed.AddListener(Possessed);
    }

    public void Possessed(User user)
    {
        Debug.Log($"{user.Name} possessed avatar {AvatarID}");
    }
}
