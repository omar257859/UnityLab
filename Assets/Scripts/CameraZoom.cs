using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lab 4 (Exercise #13)
public class CameraZoom : MonoBehaviour
{
    private Camera Cam;

    public float TargetZoom = 3; //The value of Zoom I want by manipulating the Camera Size

    private float ScrollData; //Float collected upon Mouse Scrolling

    public float ZoomSpeed = 3; //Speed of zooming process in or out

    void Start()
    {
        Cam = GetComponent<Camera>();

        TargetZoom = Cam.orthographicSize; //The game will begin with TargetZoom being assigned the default given Camera size
    }

    // Update is called once per frame
    void Update()
    {
        //when the Mouse is still, this function returns 0. Forward scrolling returns a +ve value, Backward scrolling returns -ve value.
        ScrollData = Input.GetAxis("Mouse ScrollWheel");

        //Update the Target Zoom to change from the default to however much I'm scrolling up or down.
        TargetZoom = TargetZoom - ScrollData;

        //Clamp the Camera and set limits to avoid zooming in or out too much
        TargetZoom = Mathf.Clamp(TargetZoom, 3, 6);

        //Lerp function to smooth the transition from size 1 to size 2, AKA from current Orthographic Size to Target Orthographic Size
        Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, TargetZoom, Time.deltaTime * ZoomSpeed);
    }
}
