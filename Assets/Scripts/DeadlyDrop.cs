using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lab 5 (Exercise #15, Step 9) - attached to the Lake (and the bottomless pit)
public class DeadlyDrop : MonoBehaviour
{
    //function that executes upon trigger (when something collides with the lake)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
            //if the collider of the Player touches the lake collider

            FindObjectOfType<LevelManager>().RespawnPlayer();
        //go to the Level Manager script, and execute the Respawn Player function
    }
}
