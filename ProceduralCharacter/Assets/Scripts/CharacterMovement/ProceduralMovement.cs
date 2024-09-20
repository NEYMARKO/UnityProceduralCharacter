using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProceduralMovement : MonoBehaviour
{
    PlayerInput playerInput;

    // movement
    [Header("Movement variables")]
    [SerializeField]
    float movementSpeed;

    [SerializeField]
    float rotationSpeed;

    Vector2 movementDirection;

    private InputAction movementAction;

    private void Awake()
    {
        playerInput = new PlayerInput();
        movementAction = playerInput.Player.Move;
    }
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }

    void Move()
    {
        movementDirection = movementAction.ReadValue<Vector2>();   
        if (movementDirection != Vector2.zero)
        {
            transform.position += new Vector3(movementDirection.x, 0, movementDirection.y).normalized * movementSpeed * Time.deltaTime;
            Debug.Log("MOVEMENT DIRECTION: " +  movementDirection);
        }
    }

    private void OnEnable()
    {
        movementAction.Enable();
    }

    private void OnDisable()
    {
        movementAction.Disable();
    }
}
