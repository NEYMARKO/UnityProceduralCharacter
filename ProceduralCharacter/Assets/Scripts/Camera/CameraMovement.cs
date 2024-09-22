using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    private GameObject parentObject;
    //private ProceduralMovement parent;
    Vector2 lookDirection;
    bool cameraMoved = false;

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
        parentObject = transform.parent.gameObject;
        //parent = gameObject.GetComponent<ProceduralMovement>();
    }
    private void Update()
    {
        FollowTarget();
        LookAround();
    }

    private void FollowTarget()
    {
        transform.position = parentObject.transform.position + Quaternion.Euler(pitch, yaw, 0).normalized * (Vector3.forward * cameraOffset.z);
        transform.position += new Vector3(0, cameraOffset.y, 0);
    }

    public void LookAround()
    {
        lookDirection = lookAroundAction.ReadValue<Vector2>();

        if (lookDirection != Vector2.zero)
        {
            transform.rotation = Quaternion.Euler(new Vector3(pitch, yaw, 0));
            ModifyRotationValues();
            FollowTarget();
            SetCameraMoved(true);
        }
    }

    private void ModifyRotationValues()
    {
        yaw += lookDirection.x * yawRotationSpeed * Time.deltaTime * controlMultiplier;
        pitch += lookDirection.y * pitchRotationSpeed * Time.deltaTime;
        //Debug.Log("YAW: " + yaw);
        //Debug.Log("PITCH: " + pitch);
        yaw %= 360f;
        pitch %= 360f;
        //yaw = Mathf.Clamp(yaw, -360f, 360f);
        pitch = Mathf.Clamp(pitch, minPitchValue, maxPitchValue);
    }
    public void SetCameraMoved(bool hasMoved)
    {
        cameraMoved = hasMoved;
        if (!cameraMoved)
        {
            //Debug.Log("CAMERA YAW BEFFORE: " + transform.localEulerAngles.y);
            transform.rotation = parentObject.transform.rotation;
            //Debug.Log("PARENT YAW: " + parentObject.transform.localEulerAngles.y);
            //Debug.Log("CAMERA YAW AFTER: " + transform.localEulerAngles.y);
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

    public void LockCameraRotation(Quaternion followRotation)
    {
        transform.rotation = followRotation * transform.rotation;
        accumulatedRotation = followRotation * accumulatedRotation;

        Debug.Log("ACCUMULATED ROTATION FROM MOVEMENT: " +  accumulatedRotation.eulerAngles.y);
        //yaw = transform.localEulerAngles.y;
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
