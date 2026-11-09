using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lab 2 (Exercise #06) + Lab 3 (Exercises #08, #09)
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 7; //how fast the character moves
    public float jumpHeight = 10; //how high the character jumps
    public KeyCode Spacebar = KeyCode.Space; //Jump is the name we gave a keyboard button we chose to be the jump button
    public KeyCode L = KeyCode.LeftArrow; //L is the keyboard button we chose to be the left movement button
    public KeyCode R = KeyCode.RightArrow; //R is the keyboard button we chose to be the right movement button

    public Transform groundCheck; //an invisible point in space. We use it to see if the player is touching the ground
    public float groundCheckRadius = 0.25f; //how big the circle around the player's feet is
    public LayerMask whatIsGround; //this variable stores what is considered a ground to the character
    private bool grounded; //check if the character is standing on solid ground

    private Animator anim; //instance of Animator object to control the character's animation in code

    void Start()
    {
        anim = GetComponent<Animator>(); //the animation will play accordingly when the character moves
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(Spacebar) && grounded) //When user presses the space button ONCE
        {
            Jump();
        }

        if (Input.GetKey(L)) //When user presses the left arrow button
        {
            GetComponent<Rigidbody2D>().velocity = new Vector2(-moveSpeed, GetComponent<Rigidbody2D>().velocity.y);
            //player character moves horizontally to the left along the x-axis without disrupting jump

            if (GetComponent<SpriteRenderer>() != null)
            {
                GetComponent<SpriteRenderer>().flipX = true;
            }
        }

        if (Input.GetKey(R)) //When user presses the right arrow button
        {
            GetComponent<Rigidbody2D>().velocity = new Vector2(moveSpeed, GetComponent<Rigidbody2D>().velocity.y);
            //player character moves horizontally to the right along the x-axis without disrupting jump

            if (GetComponent<SpriteRenderer>() != null)
            {
                GetComponent<SpriteRenderer>().flipX = false;
            }
        }

        //Lab 3: connect the character's velocity with the Animator's parameters
        anim.SetFloat("Speed", Mathf.Abs(GetComponent<Rigidbody2D>().velocity.x));
        anim.SetFloat("Height", GetComponent<Rigidbody2D>().velocity.y);
        anim.SetBool("Grounded", grounded);
    }

    void FixedUpdate()
    {
        grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        //this statement calculates when exactly the character is considered by Unity's engine to be
        //standing on the ground
    }

    void Jump()
    {
        GetComponent<Rigidbody2D>().velocity = new Vector2(GetComponent<Rigidbody2D>().velocity.x, jumpHeight);
        //player character jumps vertically along the y-axis without disrupting horizontal walk
    }
}
