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

    bool characterMoved = false;

    float thumbstickAngle = 0f;
    float lastAngle = 0f;

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
        DrawRays();
    }

    void Move()
    {
        movementDirection = movementAction.ReadValue<Vector2>();   
        if (movementDirection != Vector2.zero)
        {
            //if (_camera.CameraMoved())
            //{
            //    AlignCharacterRotationToCamera();
            //}
            //else 
            //{
            //}
            transform.position += CalculateMovementDirection() * movementSpeed * Time.deltaTime;
            SetCharacterMoved(true);
        }
        Debug.Log("CURRENT ROTATION: " +  transform.localEulerAngles.y);
        Debug.Log("TARGET ROTATION: " + targetRotation.eulerAngles.y);    
        //if (_camera.CameraMoved() &&
        if (Quaternion.Angle(transform.rotation, targetRotation) == 0f) _camera.SetCameraMoved(false);
    }

    private void RotateCharacter(float rotationAngle)
    {
        Quaternion thumbstickRotation = Quaternion.Euler(0, rotationAngle, 0).normalized;
        targetRotation = _camera.GetCameraYRotation() * thumbstickRotation;
        //AnimateRotation(transform.rotation, thumbstickRotation * transform.rotation);
        //transform.rotation = thumbstickRotation * transform.rotation;
    }
    private Vector3 CalculateMovementDirection()
    {
        // angle of thumbstick during input
        thumbstickAngle = Mathf.Atan2(movementDirection.x, movementDirection.y) * Mathf.Rad2Deg;
        //RotateCharacter(thumbstickAngle - lastAngle);
        RotateCharacter(thumbstickAngle);
        lastAngle = thumbstickAngle;
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
        //Debug.Log("ALIGN PLAYER ROTATION TO MOVEMENT");
        targetRotation = _camera.GetCameraYRotation();
        //AnimateRotation(transform.rotation, _camera.GetCameraYRotation());
        //transform.rotation = _camera.GetCameraYRotation();

        lastAngle = 0f;
        //_camera.SetCameraMoved(false);
    }

    private void AnimateRotation(Quaternion startRotation, Quaternion endRotation)
    {
        transform.rotation = Quaternion.RotateTowards(startRotation, endRotation, rotationSpeed * Time.deltaTime);
    }

    private void DrawRays()
    {
        Debug.DrawRay(transform.position, targetRotation * transform.forward * 10f, Color.blue);
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
