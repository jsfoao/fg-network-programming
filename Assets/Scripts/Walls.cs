using UnityEngine;

public class Walls : MonoBehaviour
{
    private Player owningPlayer;
    private HealthComponent health;
    private void Start()
    {
        Lobby.Instance.OnStartMatch.AddListener(SetHealthComponent);
    }
    private void SetHealthComponent()
    {
        owningPlayer = Lobby.Instance.GetPlayer(Lobby.Instance.PlayersData[0].User);
        health = owningPlayer.GetComponent<HealthComponent>();
    }
 
    private void OnCollisionEnter(Collision collision)
    {
        health.DecrementHealth();
    }
}
