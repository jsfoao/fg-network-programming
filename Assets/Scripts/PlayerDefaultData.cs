using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDefaults", menuName = "GameData/PlayerDefaults")]
public class PlayerDefaultData : ScriptableObject
{
    [SerializeField]
    public int Health = 3;
}
