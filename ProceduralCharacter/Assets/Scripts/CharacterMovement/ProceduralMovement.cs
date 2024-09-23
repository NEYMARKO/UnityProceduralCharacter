using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProceduralMovement : MonoBehaviour
{
    PlayerInput playerInput;

    // movement
    [Header("Movement")]
    [SerializeField]
    float movementSpeed;

    [Header("Rotation")]
    [SerializeField]
    float rotationSpeed;

    Vector2 movementDirection;

    private InputAction movementAction;

    [SerializeField]
    CameraMovement _camera;

    float thumbstickAngle = 0f;

    Quaternion targetRotation;

    private void Awake()
    {
        playerInput = new PlayerInput();
        movementAction = playerInput.Player.Move;
    }
    void Update()
    {
        Move();
        AnimateRotation(transform.rotation, targetRotation);
    }

    void Move()
    {
        movementDirection = movementAction.ReadValue<Vector2>();   
        if (movementDirection != Vector2.zero)
        { 
            transform.position += CalculateMovementDirection() * movementSpeed * Time.deltaTime;
        }
    }

    private void RotateCharacter(float rotationAngle)
    {
        Quaternion thumbstickRotation = Quaternion.Euler(0, rotationAngle, 0).normalized;
        targetRotation = _camera.GetCameraYRotation() * thumbstickRotation;
    }
    private Vector3 CalculateMovementDirection()
    {
        // angle of thumbstick during input
        thumbstickAngle = Mathf.Atan2(movementDirection.x, movementDirection.y) * Mathf.Rad2Deg;
        RotateCharacter(thumbstickAngle);
        return transform.forward;
    }
    
    private bool CharacterMoving()
    {
        return (movementAction.ReadValue<Vector2>() != Vector2.zero);
    }

    private void AnimateRotation(Quaternion startRotation, Quaternion endRotation)
    {
        transform.rotation = Quaternion.RotateTowards(startRotation, endRotation, rotationSpeed * Time.deltaTime);
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
