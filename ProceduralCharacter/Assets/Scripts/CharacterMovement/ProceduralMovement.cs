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

    [Header("Leg IK")]
    [SerializeField] Transform _LLegTarget;
    [SerializeField] Transform _RLegTarget;
    [SerializeField] float legMovementSpeed;
    [SerializeField] float legOffset;
    
    [Header("SphereCast emitters")]
    [SerializeField] Transform leftKnee;
    [SerializeField] Transform rightKnee;

    float hipsHeight = 1f;
    float leftLegHeightOffset;
    float rightLegHeightOffset;
    enum Leg { LEFT, RIGHT };
    Leg lastMovedLeg;
    struct ActiveLeg
    {
        public Transform transform;
        public Leg type;
    }

    ActiveLeg activeLeg;


    RaycastHit lLegHit;
    RaycastHit rLegHit;

    Transform bodyPosition;
    private void Awake()
    {
        playerInput = new PlayerInput();
        movementAction = playerInput.Player.Move;
        
        activeLeg.transform = _LLegTarget;
        activeLeg.type = Leg.LEFT;

        lastMovedLeg = Leg.LEFT;
    }
    private void Start()
    {
        leftLegHeightOffset = 0f;
        rightLegHeightOffset = 0f;
    }
    void Update()
    {
        Move();
        AnimateRotation(transform.rotation, targetRotation);
        //UpdateLegs();
        //_LLegTarget.position = new Vector3(0, 0, 2);
    }

    void Move()
    {
        movementDirection = movementAction.ReadValue<Vector2>();
        if (CharacterMoving())
        { 
            transform.position += GetForwardDirection() * movementSpeed * Time.deltaTime;
            UpdateLegs();
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

    private void UpdateLegs()
    {
        //Debug.Log($"LEFT LEG COLLIDED: {LegGrounded(leftKnee.position)}");
        //Debug.Log($"RIGHT LEG GROUNDED: {LegGrounded(rightKnee.position)}");
        lLegHit = FindGround(leftKnee.position);
        rLegHit = FindGround(rightKnee.position);

        _LLegTarget.position = new Vector3(0, 0, 0);
        _RLegTarget.position = new Vector3(0, 0, 0);
        //Debug.Log($"GROUND FOUND AT: {rLegHit.point}");
        //RepositionLegTarget(_LLegTarget, lLegHit);
        //RepositionLegTarget(_RLegTarget, rLegHit);
        //Vector3 newPos = _LLegTarget.position;
        //newPos.y = lLegHit.point.y;
        //_LLegTarget.position = newPos;
        ////_LLegTarget.position.y = lLegHit.point.y;
        //newPos = _RLegTarget.position;
        //newPos.y = rLegHit.point.y;
        //_RLegTarget.position = newPos; 
    }

    private bool LegGrounded(Vector3 legPosition)
    {
        int groundLayerMask = LayerMask.GetMask("Terrain");
        //Ray ray = new Ray(legPosition + (legType == Leg.LEFT ? lLegHit.normal : rLegHit.normal) * 0.15f, Vector3.down);
        Ray ray = new Ray(legPosition, Vector3.down);
        //return Physics.SphereCast(ray, 0.1f, 0.01f, 1 << 10);
        return Physics.SphereCast(ray, 0.1f, 2f, groundLayerMask);
    }

    private RaycastHit FindGround(Vector3 legPosition)
    {
        RaycastHit hit;
        int groundLayerMask = LayerMask.GetMask("Terrain");
        Physics.SphereCast(legPosition, 0.1f, Vector3.down, out hit, 2f, groundLayerMask);
        return hit;
    }

    private void RepositionLegTarget(Transform legTarget, RaycastHit hit)
    {
        //if there is little 
        //if (Vector3.Distance(legTarget.position, hit.point) < 0.2f) return;
        legTarget.position = hit.point;
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
