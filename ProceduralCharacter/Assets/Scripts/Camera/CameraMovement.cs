using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    private GameObject parent;

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

    Vector2 lookDirection;


    #region InputParameters
    PlayerInput playerInput;
    private InputAction lookAroundAction;
    #endregion
    private void Awake()
    {
        playerInput = new PlayerInput();
        lookAroundAction = playerInput.Player.Look;
        controlMultiplier = invertControls ? -1 : 1;
    }
    private void Start()
    {
        parent = this.transform.parent.gameObject;
    }
    private void Update()
    {
        LookAround();
    }

    private void FollowTarget(float yaw, float pitch)
    {
        transform.position = parent.transform.position + Quaternion.Euler(pitch, yaw, 0) * (Vector3.forward * cameraOffset.z);
        transform.position += new Vector3(0, cameraOffset.y);
    }

    public void LookAround()
    {
        lookDirection = lookAroundAction.ReadValue<Vector2>();

        if (lookDirection != Vector2.zero)
        {
            transform.rotation = Quaternion.Euler(new Vector3(pitch, yaw, 0));
        }
        ModifyRotationValues();
        FollowTarget(yaw, pitch);
    }

    private void ModifyRotationValues()
    {
        yaw += lookDirection.x * yawRotationSpeed * Time.deltaTime * controlMultiplier;
        pitch += lookDirection.y * pitchRotationSpeed * Time.deltaTime;

        yaw %= 360f;
        pitch %= 360f;

        pitch = Mathf.Clamp(pitch, minPitchValue, maxPitchValue);
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
