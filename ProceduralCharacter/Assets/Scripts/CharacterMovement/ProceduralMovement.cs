using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProceduralMovement : MonoBehaviour
{
    PlayerInput playerInput;

    [Header("Movement")]
    [SerializeField]
    InputAction movementAction;
    [SerializeField]
    float movementSpeed;
    Vector2 movementDirection;

    [Header("Rotation")]
    [SerializeField]
    float rotationSpeed;
    float thumbstickAngle = 0f;
    Quaternion targetRotation;

    [Header("Camera")]
    [SerializeField]
    CameraMovement _camera;

    [Header("Leg IK")]
    [SerializeField]
    Transform _LLegTarget;
    [SerializeField]
    Transform _RLegTarget;
    [SerializeField]
    float semiMinorAxis;
    [SerializeField]
    float semiMajorAxis;
    [SerializeField]
    float legOffset;
    float t = 0f;
    Vector3 pointOnEllipse;
    private void Awake()
    {
        playerInput = new PlayerInput();
        movementAction = playerInput.Player.Move;
    }
    void Update()
    {
        Move();
        AnimateRotation(transform.rotation, targetRotation);
        DrawPoints();
    }

    void Move()
    {
        movementDirection = movementAction.ReadValue<Vector2>();   
        if (movementDirection != Vector2.zero)
        { 
            transform.position += GetForwardDirection() * movementSpeed * Time.deltaTime;
            UpdateLegPosition(_LLegTarget);
            UpdateLegPosition(_RLegTarget);
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
    
    private bool CharacterMoving()
    {
        return (movementAction.ReadValue<Vector2>() != Vector2.zero);
    }

    private void AnimateRotation(Quaternion startRotation, Quaternion endRotation)
    {
        transform.rotation = Quaternion.RotateTowards(startRotation, endRotation, rotationSpeed * Time.deltaTime);
    }

    private void UpdateLegPosition(Transform legTarget)
    {
        t += movementSpeed * Time.deltaTime;
        pointOnEllipse = ConstructEllipseMovement(t);

        Debug.Log("Point on ellipse: " + pointOnEllipse);

        legTarget.position += (transform.rotation * pointOnEllipse * legOffset) * Time.deltaTime;
    }

    private Vector3 ConstructEllipseMovement(float t)
    {
        t %= (2 * Mathf.PI);
        return new Vector3(0, semiMajorAxis * Mathf.Cos(t) * Mathf.Rad2Deg, semiMinorAxis * Mathf.Sin(t) * Mathf.Rad2Deg);
    }

    private void DrawPoints()
    {
        Debug.DrawLine(pointOnEllipse, pointOnEllipse + transform.forward, Color.yellow, 50f);
        Debug.DrawLine(_LLegTarget.position, _LLegTarget.position + transform.forward, Color.red, 50f);
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
