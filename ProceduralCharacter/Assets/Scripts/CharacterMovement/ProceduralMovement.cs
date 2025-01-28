using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.ReorderableList;
using UnityEditor.Timeline;
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

    [Header("Hips")]
    [SerializeField] float hipsLoweredAmount;
    [SerializeField] float hipsAnimationSpeed;
    [Header("Legs")]
    [SerializeField] IKFootSolver leftLeg;
    [SerializeField] IKFootSolver rightLeg;
    float lerp = 0f;
    bool hipsAnimating = false;
    Vector3 oldHipsPos, currentHipsPos, newHipsPos;

    public event Action OnMovementStopped;

    private void Awake()
    {
        playerInput = new PlayerInput();
        movementAction = playerInput.Player.Move;
    }

    private void Start()
    {
        oldHipsPos = currentHipsPos = newHipsPos = transform.position;
    }
    void Update()
    {
        Move();
        AnimateRotation(transform.rotation, targetRotation);
        ModifyHipsHeight(leftLeg.GetTargetHeight(), rightLeg.GetTargetHeight());
        Vector3 temp = transform.position;
        temp.y = currentHipsPos.y;
        transform.position = temp;
        //Debug.Log($"OLD, NEW: {oldHipsPos}, {newHipsPos}");
        if (oldHipsPos.y != newHipsPos.y && !hipsAnimating)
        {
            lerp = 0f;
            hipsAnimating = true;
        }
        if (lerp < 1)
        {
            AnimateHipsHeightChange();
            lerp += hipsAnimationSpeed * Time.deltaTime;
        }
        else if (lerp >= 1 || leftLeg.animationCompleted >= 1 || rightLeg.animationCompleted >= 1)
        {
            oldHipsPos = newHipsPos;
            hipsAnimating = false;
        }
    }

    void Move()
    {
        movementDirection = movementAction.ReadValue<Vector2>();
        if (DetectedMovementInput())
        {
            UpdateForwardDirection();
            transform.position += transform.forward * movementSpeed * Time.deltaTime;
        }
        else OnMovementStopped?.Invoke();
    }

    private void RotateCharacter(float rotationAngle)
    {
        // rotation of the camera must be taken in consideration, otherwise
        // rotation would be done in the world system instead of player's
        Quaternion thumbstickRotation = Quaternion.Euler(0, rotationAngle, 0).normalized;
        targetRotation = _camera.GetCameraYRotation() * thumbstickRotation;
    }
    private void UpdateForwardDirection()
    {
        // angle of thumbstick during input
        thumbstickAngle = Mathf.Atan2(movementDirection.x, movementDirection.y) * Mathf.Rad2Deg;
        RotateCharacter(thumbstickAngle);
    }

    public bool DetectedMovementInput()
    {
        return (movementAction.ReadValue<Vector2>() != Vector2.zero);
    }

    private void AnimateRotation(Quaternion startRotation, Quaternion endRotation)
    {
        transform.rotation = Quaternion.RotateTowards(startRotation, endRotation, rotationSpeed * Time.deltaTime);
    }

    private void ModifyHipsHeight(float leg1Height, float leg2Height)
    {
        //if (!hipsAnimating) return;
        //if (lerp >= 1) return;
        //Debug.Log("LERP: " + lerp);
        newHipsPos = transform.position;
        newHipsPos.y = Mathf.Min(leg1Height, leg2Height) - hipsLoweredAmount;
        //Debug.Log($"LEFT HEIGHT, RIGHT HEIGHT: ({leg1Height}, {leg2Height})");
        //newHipsPos.y = leftLeg.MovingUp() ? Mathf.Max(leg1Height, leg2Height) - hipsLoweredAmount : Mathf.Min(leg1Height, leg2Height) - hipsLoweredAmount;
        //transform.position = newHipsPos;
    }
    private void AnimateHipsHeightChange()
    {
        //currentHipsPos = transform.position;
        currentHipsPos = Vector3.Lerp(oldHipsPos, newHipsPos, lerp);
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

    public Quaternion GetPlayerRotation()
    {
        return transform.rotation;
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
