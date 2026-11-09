using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collect : MonoBehaviour
{


    public AudioClip coin1;
    public AudioClip coin2;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            PlayerStats.score++;

           AudioManager.Instance.PlayMusicSFX(coin1);

            Debug.Log("Score: " + PlayerStats.score);

            Destroy(gameObject);
        }
    }

}
