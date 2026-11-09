using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lab 4 (Exercise #12)
public class CameraFollow : MonoBehaviour
{
    public Transform Target;
    public float Cameraspeed = 6;

    //Determine the minimum and maximum values in both directions where the camera can move. This is to stop it
    //from showing game edges or things you don't want to appear to the player
    public float minX, maxX;
    public float minY, maxY;

    void Start()
    {
    }

    void FixedUpdate()
    {
        if (Target != null)
        {
            Vector2 newCamPosition = Vector2.Lerp(transform.position, Target.position, Time.deltaTime * Cameraspeed);

            float ClampX = Mathf.Clamp(newCamPosition.x, minX, maxX);
            float ClampY = Mathf.Clamp(newCamPosition.y, minY, maxY);

            transform.position = new Vector3(ClampX, ClampY, -10f);
        }
    }
}
