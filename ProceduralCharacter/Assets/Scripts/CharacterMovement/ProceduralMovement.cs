using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.ReorderableList;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings.Switch;

public class ProceduralMovement : MonoBehaviour
{
    PlayerInput playerInput;

    [Header("Movement")]
    [SerializeField] InputAction movementAction;
    [SerializeField] float movementSpeed;
    Vector2 movementDirection;

    [Header("Rotation")]
    [SerializeField] float rotationSpeed;
    float thumbstickAngle = 0f;
    Quaternion targetRotation;

    [Header("Camera")]
    [SerializeField] CameraMovement _camera;

    bool movementStopped = true;
    bool startedMoving = false;

    //float hipsHeight = 1f;
   
    private void Awake()
    {
        playerInput = new PlayerInput();
        movementAction = playerInput.Player.Move;
    }
    void Update()
    {
        Move();
        ToggleStandingStill();
        AnimateRotation(transform.rotation, targetRotation);
    }

    void Move()
    {
        movementDirection = movementAction.ReadValue<Vector2>();
        if (DetectedMovementInput())
        { 
            transform.position += GetForwardDirection() * movementSpeed * Time.deltaTime;
        }
    }

    private void RotateCharacter(float rotationAngle)
    {
        // rotation of the camera must be taken in consideration, otherwise
        // rotation would be done in the world system instead of player's
        Quaternion thumbstickRotation = Quaternion.Euler(0, rotationAngle, 0).normalized;
        targetRotation = _camera.GetCameraYRotation() * thumbstickRotation;
    }
    private Vector3 GetForwardDirection()
    {
        // angle of thumbstick during input
        thumbstickAngle = Mathf.Atan2(movementDirection.x, movementDirection.y) * Mathf.Rad2Deg;
        RotateCharacter(thumbstickAngle);
        return transform.forward;
    }
    
    public bool DetectedMovementInput()
    {
        return (movementAction.ReadValue<Vector2>() != Vector2.zero);
    }

    private void AnimateRotation(Quaternion startRotation, Quaternion endRotation)
    {
        transform.rotation = Quaternion.RotateTowards(startRotation, endRotation, rotationSpeed * Time.deltaTime);
    }

    private void ToggleStandingStill()
    {
        switch(DetectedMovementInput())
        {
            case true:
                if (movementStopped) startedMoving = true;
                movementStopped = false;
                break;
            case false:
                movementStopped = true;
                startedMoving = false;
                break;
            default:

        }
        //if (standingStill && DetectedMovementInput())
        //{
        //    standingStill = false;
        //    startedMoving = true;
        //}
        //else
        //{
        //    standingStill = true;
        //    startedMoving = false;
        //}
    }
    public float GetMovementSpeed()
    {
        return movementSpeed;
    }

    public float GetMovementMagnitude()
    {
        return movementDirection.magnitude;
    }

    public float GetScaledMovementSpeed()
    {
        return movementSpeed * movementDirection.magnitude;
    }

    public bool PlayerMoving()
    {
        return movementStopped;
    }

    public bool MovementStarted()
    {
        return movementStopped;
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
