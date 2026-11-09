using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lab 5 (Exercise #15, Step 7)
public class Checkpoint : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
            //if the collider of the Player touches the checkpoint's circle collider

            FindObjectOfType<LevelManager>().CurrentCheckpoint = this.gameObject;
        //go to the Level Manager script, and update the value of CurrentCheckpoint to become the new Checkpoint the player has
        //just passed through. This is necessary when you have several checkpoints in a level
    }
}
