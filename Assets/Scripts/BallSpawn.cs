using Alteruna;
using UnityEngine;

public class BallSpawn : MonoBehaviour
{
    [SerializeField] private Spawner spawner;
    [SerializeField] private Vector3 spawnPos;
    private GameObject ball;

    public void SpawnBall()
    {
        if (Lobby.Instance.IsAdmin())
        {
            ball = spawner.Spawn(0, spawnPos);
        }
    }

    public void RemoveBall()
    {
        if (ball != null)
        {
            spawner.Despawn(ball);
        }
    }
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.B))
        //{
        //    spawner.Spawn(0, spawnPos);
        //}
    }
    
}
