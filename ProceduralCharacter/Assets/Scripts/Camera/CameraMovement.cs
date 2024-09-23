using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    //private ProceduralMovement parent;
    Vector2 lookDirection;
    bool cameraMoved = false;

    [SerializeField]
    Transform target;

    [Header("Position")]
    [SerializeField]
    Vector3 cameraOffset;

    #region RotationParameters
    [Header("Rotation")]
    [SerializeField]
    float yawRotationSpeed;
    [SerializeField]
    float pitchRotationSpeed;
    [SerializeField]
    float minPitchValue;
    [SerializeField]
    float maxPitchValue;
    [SerializeField]
    bool invertControls;
    float yaw = 0f;
    float pitch = 0f;
    int controlMultiplier = 1;
    #endregion

    #region InputParameters
    PlayerInput playerInput;
    private InputAction lookAroundAction;
    #endregion

    private void Awake()
    {
        playerInput = new PlayerInput();
        lookAroundAction = playerInput.Player.Look;
        controlMultiplier = invertControls ? 1 : -1;
    }
    private void Update()
    {
        FollowTarget();
        LookAround();
        DrawRays();
    }

    private void FollowTarget()
    {
        transform.position = target.position + Quaternion.Euler(pitch, yaw, 0).normalized * (Vector3.forward * cameraOffset.z);
        transform.position += new Vector3(0, cameraOffset.y, 0);
    }

    public void LookAround()
    {
        lookDirection = lookAroundAction.ReadValue<Vector2>();

        if (lookDirection != Vector2.zero)
        {
            ModifyRotationValues();
            transform.rotation = Quaternion.Euler(new Vector3(pitch, yaw, 0));
            SetCameraMoved(true);
        }
    }

    private void ModifyRotationValues()
    {
        yaw += lookDirection.x * yawRotationSpeed * Time.deltaTime * controlMultiplier;
        pitch += lookDirection.y * pitchRotationSpeed * Time.deltaTime;
       
        yaw %= 360f;
        pitch %= 360f;

        pitch = Mathf.Clamp(pitch, minPitchValue, maxPitchValue);
    }
    public void SetCameraMoved(bool hasMoved)
    {
        cameraMoved = hasMoved;
        if (!cameraMoved)
        {
            //transform.rotation = target.rotation;
        }
    }
    
    public bool CameraMoved()
    {
        return cameraMoved;
    }

    public Quaternion GetCameraYRotation()
    {
        return Quaternion.Euler(new Vector3(0, yaw, 0));
    }

    private void DrawRays()
    {
        //Debug.DrawRay(target.position, target.rotation * target.forward * 10f, Color.red);
        Debug.DrawRay(target.position, transform.rotation * target.forward * 10f, Color.green);
    }
    private void OnEnable()
    {
        lookAroundAction.Enable();
    }

    private void OnDisable()
    {
        lookAroundAction.Disable();
    }
}
