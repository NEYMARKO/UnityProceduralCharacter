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
            //if (transform.forward != _camera.transform.forward) transform.rotation *= _camera.GetCameraYRotation();
            float angle = Mathf.Atan2(movementDirection.x, movementDirection.y) * Mathf.Rad2Deg;
            Debug.Log("ANGLE: " + angle);
            Quaternion thumbstickRotation = Quaternion.Euler(0, angle, 0);
            transform.position += (thumbstickRotation * transform.forward).normalized * movementSpeed * Time.deltaTime;
            //Debug.Log("MOVEMENT DIRECTION: " + movementDirection);
        }
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
