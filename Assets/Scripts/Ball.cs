using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{

    public float speed;
    public Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        LaunchBall();
    }

    //update is called once per frame
    void update()
    {

    }

    private void LaunchBall()
    {
        float x = Random.Range(0, 2) == 0 ? -1 : 1;
        float y = Random.Range(0, 2) == 0 ? -1 : 1;
        Vector2 direction = new Vector2(x,y);
        rb.AddForce(direction * this.speed);
    }


}
