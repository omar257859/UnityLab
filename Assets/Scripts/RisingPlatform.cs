using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lab 5 (Optional Exercise - Moving Platform)
public class RisingPlatform : MonoBehaviour
{
    //platform will move continuously between these 2 points:
    public Transform PointA;
    public Transform PointB;

    public float speed = 2;
    private bool AtoB;

    private Vector3 targetPosition;

    void Update()
    {
        //The platform should always start its journey from Point A to Point B.
        //Once AtoB eventually becomes false, the platform will begin moving back towards Point A
        if (AtoB == true)
        {
            targetPosition = PointA.position;
        }
        else
        {
            targetPosition = PointB.position;
        }

        //Move towards target position on Y-axis
        Vector3 newPosition = transform.position;
        newPosition.y = Mathf.MoveTowards(transform.position.y, targetPosition.y, speed * Time.deltaTime);

        transform.position = newPosition;

        //Switch direction when closing in on target
        if (Mathf.Abs(transform.position.y - targetPosition.y) < 0.1f)
        {
            AtoB = !AtoB; //if true, become false. If false, become true
        }
    }

    //To make the character sit still on the platform, we will make him a child as soon as he stands
    //on it
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            //Make the platform a parent
            collision.transform.SetParent(transform);
        }
    }

    //The moment the character jumps, he's no longer a child of the platform
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            //Unparent the player
            collision.transform.SetParent(null);
        }
    }
}
