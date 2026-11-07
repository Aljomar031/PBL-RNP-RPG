using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterController : MonoBehaviour
{
    // Movement speeds
    public float movespeed = 5f;
    public float runspeed = 8f;

    public static bool isFacingRight;

    // Components used by the character
    public Rigidbody2D myRigidbody;
    public Collider2D myCollider;
    public Animator myAnimator;
    public SpriteRenderer mySpriteRenderer;

    // Checks if the Run button is being held
    public bool isRunning = false;

    // Available movement directions
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    // Stores the directions currently being held
    private List<Direction> heldDirections = new List<Direction>();

    // Stores the current animation to prevent restarting it
    private string currentAnimation = "";

    void Start()
    {
        // Gets the components attached to the player
        myRigidbody = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Checks which keyboard directions are being pressed, for pc support dawg
        CheckKeyboard(KeyCode.W, Direction.Up);
        CheckKeyboard(KeyCode.S, Direction.Down);
        CheckKeyboard(KeyCode.A, Direction.Left);
        CheckKeyboard(KeyCode.D, Direction.Right);

        // Moves in the most recently pressed direction
        if (heldDirections.Count > 0)
        {
            Direction direction = heldDirections[heldDirections.Count - 1];
            MoveCharacter(direction);
        }
        else
        {
            // Stops the character when no direction is held
            myRigidbody.linearVelocity = Vector2.zero;
            PlayAnimation("Idle");
        }
    }

    void CheckKeyboard(KeyCode key, Direction direction)
    {
        // Adds the direction when the key is pressed
        if (Input.GetKeyDown(key))
        {
            PressDirection(direction);
        }

        // Removes the direction when the key is released
        if (Input.GetKeyUp(key))
        {
            ReleaseDirection(direction);
        }
    }

    public void PressDirection(Direction direction)
    {
        // Moves the direction to the end of the list
        heldDirections.Remove(direction);
        heldDirections.Add(direction);
    }

    public void ReleaseDirection(Direction direction)
    {
        // Removes the released direction
        heldDirections.Remove(direction);
    }

    public void StartRunning()
    {
        // Enables running while the Run button is held
        isRunning = true;
    }

    public void StopRunning()
    {
        // Returns to walking speed after releasing the button
        isRunning = false;
    }

    void MoveCharacter(Direction direction)
    {
        // Stores the movement direction
        Vector2 velocity = Vector2.zero;

        switch (direction)
        {
            case Direction.Up:
                velocity.y = 1f;
                PlayAnimation("Walk_Up");
                break;

            case Direction.Down:
                velocity.y = -1f;
                PlayAnimation("Walk_Down");
                break;

            case Direction.Left:
                velocity.x = -1f;
                mySpriteRenderer.flipX = false;
                isFacingRight = false;
                PlayAnimation("Walk_Left");
                break;

            case Direction.Right:
                velocity.x = 1f;
                mySpriteRenderer.flipX = true;
                isFacingRight = true;
                PlayAnimation("Walk_Left");
                break;
        }

        // Chooses walking or running speed
        float speed = isRunning ? runspeed : movespeed;

        // Applies movement using the selected speed
        myRigidbody.linearVelocity = velocity * speed;
    }

    void PlayAnimation(string animationName)
    {
        // Plays the animation only when it changes
        if (currentAnimation != animationName)
        {
            myAnimator.Play(animationName);
            currentAnimation = animationName;
        }
    }
}