using Alteruna;
using UnityEngine;

public class BallSpawn : MonoBehaviour
{
    [SerializeField] private Spawner spawner;
    [SerializeField] private Vector3 spawnPos;
    
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            spawner.Spawn(0, spawnPos);
        }
    }
    
}
