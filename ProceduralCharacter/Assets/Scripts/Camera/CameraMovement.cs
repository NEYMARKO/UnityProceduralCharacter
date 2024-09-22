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
    GameObject targetObject;

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

    Quaternion followRotation;
    Quaternion accumulatedRotation = Quaternion.identity;
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
    private void Start()
    {
        //parent = gameObject.GetComponent<ProceduralMovement>();
    }
    private void Update()
    {
        FollowTarget();
        LookAround();
        DrawRays();
    }

    private void FollowTarget()
    {
        transform.position = targetObject.transform.position + Quaternion.Euler(pitch, yaw, 0).normalized * (Vector3.forward * cameraOffset.z);
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
            transform.rotation = targetObject.transform.rotation;
            accumulatedRotation = Quaternion.identity;
        }
    }
    
    public bool CameraMoved()
    {
        return cameraMoved;
    }

    public Quaternion GetCameraYRotation()
    {
        // cancel effect of accumulated rotation got from locking camera relative rotation
        //return Quaternion.Inverse(accumulatedRotation) * Quaternion.Euler(new Vector3(0, yaw, 0));
        return Quaternion.Euler(new Vector3(0, yaw, 0));
    }

    public void LockCameraRotation(Quaternion playerRotation)
    {
        // rotate camera opposite to player rotation direction to keep it focused on player
        followRotation = Quaternion.Inverse(playerRotation);
        //transform.rotation = followRotation * transform.rotation;
        accumulatedRotation = followRotation * accumulatedRotation;
    }

    private void DrawRays()
    {
        float length = 10f;
        Quaternion accumulatedDirection = Quaternion.Inverse(accumulatedRotation) * Quaternion.Euler(new Vector3(0, yaw, 0));
        Debug.DrawRay(targetObject.transform.position, accumulatedDirection * transform.forward * length, Color.red);
        Quaternion cameraRotation = Quaternion.Euler(new Vector3(0, yaw, 0));
        Debug.DrawRay(targetObject.transform.position, cameraRotation * transform.forward * length, Color.green);
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
