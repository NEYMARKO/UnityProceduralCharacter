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

    CameraMovement _camera;

    bool characterMoved = false;

    float thumbstickAngle = 0f;
    float lastAngle = 0f;

    private void Awake()
    {
        playerInput = new PlayerInput();
        movementAction = playerInput.Player.Move;
        _camera = GetComponentInChildren<CameraMovement>();
    }
    void Start()
    {
    }

    void Update()
    {
        Move();
        //if (!CharacterMoving()) lastAngle = 0f;
    }

    void Move()
    {
        movementDirection = movementAction.ReadValue<Vector2>();   
        if (movementDirection != Vector2.zero)
        {
            if (_camera.CameraMoved())
            {
                AlignCharacterRotationToCamera();
                _camera.SetCameraMoved(false);
            }

            transform.position += CalculateMovementDirection() * movementSpeed * Time.deltaTime;
            SetCharacterMoved(true);
        }
    }

    private void RotateCharacter(float rotationAngle)
    {
        Quaternion thumbstickRotation = Quaternion.Euler(0, rotationAngle, 0).normalized;
        transform.rotation = thumbstickRotation * transform.rotation;
        _camera.LockCameraRotation(Quaternion.Inverse(thumbstickRotation));
    }
    private Vector3 CalculateMovementDirection()
    {
        // angle of thumbstick during input
        thumbstickAngle = Mathf.Atan2(movementDirection.x, movementDirection.y) * Mathf.Rad2Deg;
        //Debug.Log("ROTATION AMOUNT: " + (thumbstickAngle - lastAngle)); 
        RotateCharacter(thumbstickAngle - lastAngle);
        lastAngle = thumbstickAngle;
        // player forward actor rotated to move in direction of thumbstick relative to it's own rotation
        //return (thumbstickRotation * transform.forward).normalized * movementSpeed * Time.deltaTime;
        return transform.forward;
    }
    
    private bool CharacterMoving()
    {
        return (movementAction.ReadValue<Vector2>() != Vector2.zero);
    }
    public void SetCharacterMoved(bool hasMoved)
    {
        characterMoved = hasMoved;
    }

    public bool GetCharacterMoved()
    {
        return characterMoved;
    }
    private void AlignCharacterRotationToCamera()
    {
        transform.rotation = _camera.GetCameraYRotation();
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
