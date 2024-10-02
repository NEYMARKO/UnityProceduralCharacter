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
    Transform stepTarget;
    [SerializeField]
    float semiMinorAxis;
    [SerializeField]
    float semiMajorAxis;
    [SerializeField]
    float legMovementSpeed;
    [SerializeField]
    float legOffset;
    float lT = 0f;
    float rT = 0f;
    Vector3 lPointOnEllipse;
    Vector3 rPointOnEllipse;
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
        stepTarget.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + semiMajorAxis * Mathf.Cos(0) * Mathf.Rad2Deg * legMovementSpeed);
    }
    void Update()
    {
        Move();
        AnimateRotation(transform.rotation, targetRotation);
        //DrawPoints();
        UpdateLegs();
        //_LLegTarget.position = new Vector3(0, 0, 2);
    }

    void Move()
    {
        movementDirection = movementAction.ReadValue<Vector2>();   
        if (movementDirection != Vector2.zero)
        { 
            transform.position += GetForwardDirection() * movementSpeed * Time.deltaTime;
            UpdateLegs();
            //UpdateLegPosition(_RLegTarget);
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

    private void UpdateLegPosition(ActiveLeg activeLeg)
    {
        lPointOnEllipse = ConstructEllipseMovement(activeLeg.type == Leg.LEFT ? lT : rT);
        activeLeg.transform.position += (transform.rotation * lPointOnEllipse * legMovementSpeed) * Time.deltaTime;
    }

    private Vector3 ConstructEllipseMovement(float t)
    {
        t += movementSpeed * Time.deltaTime;
        t %= (2 * Mathf.PI);
        return new Vector3(0, semiMajorAxis * Mathf.Cos(t) * Mathf.Rad2Deg, semiMinorAxis * Mathf.Sin(t) * Mathf.Rad2Deg);
    }

    private void UpdateLegs()
    {
        Debug.Log($"LEFT LEG COLLIDED: {LegGrounded(_LLegTarget.position, Leg.LEFT)}");
        Debug.Log($"RIGHT LEG COLLIDED: {LegGrounded(_RLegTarget.position, Leg.RIGHT)}");
        lLegHit = FindGround(_LLegTarget.position, Leg.LEFT);
        rLegHit = FindGround(_RLegTarget.position, Leg.RIGHT);
        Vector3 newPos = _LLegTarget.position;
        newPos.y = lLegHit.point.y;
        _LLegTarget.position = newPos;
        //_LLegTarget.position.y = lLegHit.point.y;
        newPos = _RLegTarget.position;
        newPos.y = rLegHit.point.y;
        _RLegTarget.position = newPos; 
    }

    private void UpdateStepTarget()
    {
        //Vector3 nextPosition = new Vector3 (0, 0, semiMajorAxis * Mathf.Cos(0) * Mathf.Rad2Deg);
        Vector3 nextPosition = transform.position;
        nextPosition.z += semiMajorAxis * Mathf.Cos(0) * Mathf.Rad2Deg * legMovementSpeed * Time.deltaTime;
        stepTarget.position = nextPosition;
        //stepTarget.position = (transform.rotation * nextPosition * legMovementSpeed) * Time.deltaTime;
    }

    private bool LegGrounded(Vector3 legPosition, Leg legType)
    {
        int groundLayerMask = LayerMask.GetMask("Terrain");
        //Ray ray = new Ray(legPosition + (legType == Leg.LEFT ? lLegHit.normal : rLegHit.normal) * 0.15f, Vector3.down);
        Ray ray = new Ray(legPosition + new Vector3(0, 0.15f, 0), Vector3.down);
        //return Physics.SphereCast(ray, 0.1f, 0.01f, 1 << 10);
        return Physics.SphereCast(ray, 0.1f, 2f, groundLayerMask);
    }

    private RaycastHit FindGround(Vector3 legPosition, Leg legType)
    {
        RaycastHit hit;
        int groundLayerMask = LayerMask.GetMask("Terrain");
        //Vector3 newLegPos = legPosition + (legType == Leg.LEFT ? lLegHit.normal : rLegHit.normal) * 0.15f;
        Vector3 newLegPos = legPosition + new Vector3(0, 0.15f, 0);
        //newLegPos.y += 0.2f;
        Physics.SphereCast(newLegPos, 0.1f, Vector3.down, out hit, 2f, groundLayerMask);
        return hit;
    }

    private void DrawPoints()
    {
        Debug.DrawLine(lPointOnEllipse, lPointOnEllipse + transform.forward, Color.yellow, 50f);
        Debug.DrawLine(_LLegTarget.position, _LLegTarget.position + transform.forward, Color.red, 50f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(lLegHit.point, 0.1f);
        Gizmos.DrawSphere(rLegHit.point, 0.1f);
        //Gizmos.DrawSphere(_LLegTarget.position + new Vector3(0, 0.1f, 0), 0.1f);
        //Gizmos.DrawRay(_LLegTarget.position + new Vector3(0, 0.1f, 0), Vector3.down);
        //Gizmos.DrawSphere(stepTarget.position, 0.1f);
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
