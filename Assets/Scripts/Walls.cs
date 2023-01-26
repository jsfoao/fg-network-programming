using UnityEngine;

public class Walls : MonoBehaviour
{
    [SerializeField] private int PlayerIndex;   
    private Player owningPlayer;
    private HealthComponent health;
    private void Start()
    {
        Lobby.Instance.OnStartMatch.AddListener(SetHealthComponent);
    }
    private void SetHealthComponent()
    {
        owningPlayer = Lobby.Instance.GetPlayer(Lobby.Instance.PlayersData[PlayerIndex].User);
        health = owningPlayer.GetComponent<HealthComponent>();
    }
 
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<Ball>())
        {
            health.DecrementHealth(owningPlayer);
        }
    }
}
