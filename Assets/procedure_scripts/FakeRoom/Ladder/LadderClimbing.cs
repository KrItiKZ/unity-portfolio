using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class LadderClimbing : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float climbSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private PlayerInput playerInput;
    private InputAction moveAction;

    private bool isOnLadder = false;
    private Ladder currentLadder;
    private Vector3 climbVelocity;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        
        moveAction = playerInput.actions["Move"];
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    private void Update()
    {
        if (isOnLadder)
        {
            ClimbLadder();
        }
        else
        {
            ApplyGravity();
        }

        
        controller.Move(climbVelocity * Time.deltaTime);
    }

    private void ClimbLadder()
    {
        
        Vector2 input = moveAction.ReadValue<Vector2>();
        float climbInput = input.y; 

        
        climbVelocity = currentLadder.direction.normalized * (climbInput * climbSpeed);

        
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = 0f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        climbVelocity = new Vector3(0, verticalVelocity, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        
        Ladder ladder = other.GetComponent<Ladder>();
        if (ladder != null)
        {
            isOnLadder = true;
            currentLadder = ladder;
            verticalVelocity = 0f; 
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Ladder ladder = other.GetComponent<Ladder>();
        if (ladder != null && ladder == currentLadder)
        {
            isOnLadder = false;
            currentLadder = null;
        }
    }
}