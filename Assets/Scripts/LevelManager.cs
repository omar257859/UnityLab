using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lab 5 (Exercise #15)
public class LevelManager : MonoBehaviour
{
    public GameObject CurrentCheckpoint; //so we can update the current checkpoint from within Unity

    private Vector3 startPosition; //where the player respawns if they die before reaching any checkpoint

    // Use this for initialization
    void Start()
    {
        CurrentCheckpoint = null;

        startPosition = FindObjectOfType<PlayerController>().transform.position;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void RespawnPlayer()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
            return;

        //Search for the object with the PlayerController script. Once you've found it, change its position
        //to be at the last checkpoint the player passed through before s/he died
        if (CurrentCheckpoint != null)
            player.transform.position = CurrentCheckpoint.transform.position;
        else
            player.transform.position = startPosition;

        player.GetComponent<Rigidbody2D>().velocity = Vector2.zero; //don't keep the falling speed after respawning
    }
}
