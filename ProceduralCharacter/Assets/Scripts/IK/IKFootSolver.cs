using System.Collections;
using System.Collections.Generic;
using System.Security;
using TreeEditor;
using Unity.Properties;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngineInternal;

public class IKFootSolver : MonoBehaviour
{
    [SerializeField] LayerMask terrainLayer;
    [SerializeField] bool startingLeg = false;
    [Header("Bones")]
    [SerializeField] Transform[] bones;
    float chainLength;
    [Header("Scripts")]
    [SerializeField] ProceduralMovement proceduralMovement;
    [SerializeField] IKFootSolver otherFoot;
    [Header("Feet Parameters")]
    [SerializeField] Transform footTransform;
    [SerializeField] float speed = 1f;
    [SerializeField] float stepDistance = 1f;
    [SerializeField] float stepLength = 1f;
    [SerializeField] float stepHeight = 1f;
    [SerializeField] float footHeightOffset = 0.1f;
    [SerializeField] float kneeHeightOffset;
    [SerializeField] float angleTolerance = 5f;
    //foot side offset from the center of the body
    float footSideOffset;
    float sphereCastRadius = 0.05f;
    public Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    Transform body;
    public float animationCompleted;
    bool previouslyMoved = false;
    RaycastHit hit, bodyAlignedHit, recoveryHit;
    Quaternion defaultToeRotation, footRotator = Quaternion.identity;
    [Header("Recovery")]
    [SerializeField] float recoverySpeed;
    Vector3 previousForwardDirection;
    Vector3 oldBodyPos;

    [Header("Debugging")]
    [SerializeField] Color movementBoxColor;
    [SerializeField] Color movementBoxBoundingLineColor;
    Vector3 movementBoxDimensions;

    Vector3 helperNewPos;
    Vector3 helperOldPos;
    float lineLength;

    private Vector3 initialFootOffset;
    private Vector3 localNewPosition, localOldPosition;

    struct Plane
    {
        public Vector3 normal;
        public float D;
    }
    Plane intersectionPlane;

    float oldHeight = 0f;
    RaycastHit nHit;

    void Start()
    {
        footSideOffset = transform.localPosition.x;
        body = proceduralMovement.transform;

        chainLength = CalculateIKChainLength();
        intersectionPlane = new Plane();
        recoveryHit = new RaycastHit();
        if (startingLeg)
        {
            transform.position = body.position + body.right * footSideOffset - body.forward * stepLength;
        }
        else
        {
            transform.position = body.position + body.right * footSideOffset + body.forward * stepLength;
            previouslyMoved = true;
        }

        currentPosition = oldPosition = newPosition = transform.position;
        currentNormal = oldNormal = newNormal = Vector3.up;
        oldBodyPos = body.position;
        animationCompleted = 1f;

        helperOldPos = oldPosition;

        defaultToeRotation = footTransform.rotation;
    }

    void Update()
    {
        //Debug.Log($"COLLIDER NAME: {(recoveryHit.collider == null ? "null" : recoveryHit.collider.name)}");
        transform.position = recoveryHit.collider != null ? recoveryHit.point : currentPosition + footRotator * Vector3.up * footHeightOffset;
        UpdateBodyAlignedHit();

        if (ShouldMove())
        {
            RaycastHit oHit;
            previouslyMoved = true;
            animationCompleted = 0f;
            previousForwardDirection = body.forward;
            otherFoot.previousForwardDirection = body.forward;
            oldBodyPos = body.position;
            otherFoot.oldBodyPos = oldBodyPos;
            FindHit(GetMovingFootRayCastPosition(stepLength));
            newPosition = hit.point;
            newNormal = hit.normal;
            helperNewPos = bodyAlignedHit.point + body.forward * (1 - animationCompleted) * (proceduralMovement.GetMovementSpeed() / speed + stepLength);

            Physics.SphereCast(footTransform.position + Vector3.up * footHeightOffset, sphereCastRadius, Vector3.down, out oHit, 1.5f, terrainLayer.value);
            oldPosition = oHit.point;
            helperOldPos = oldPosition;

            //IT IS SAVED RELATIVE TO THE BODY IN THE POINT IN TIME WHEN BODY HASN'T STARTED MOVING - IT WILL ALWAYS BE RIGHT NEXT TO BODY WHEN CONVERTED TO WORLD COORDINATES
            //IT SHOULD BE SAVED RELATIVE TO NEW POSITION - THERE NEEDS TO BE TRANSFORM THAT HAS POSITION OF NEWPOSITION
            localOldPosition = body.InverseTransformPoint(helperOldPos);

        }
        if (animationCompleted < 1f && previouslyMoved)
        {

            Physics.SphereCast(oldBodyPos + body.right * footSideOffset + Vector3.up * kneeHeightOffset + body.forward * (proceduralMovement.GetMovementSpeed() / speed + stepLength)
                , sphereCastRadius, Vector3.down, out nHit, 1.5f, terrainLayer.value);

            helperNewPos = nHit.point;
            helperOldPos = body.TransformPoint(localOldPosition);

            if (proceduralMovement.DetectedMovementInput())
            {
                AnimateStep();
                AnimateFootRotation();
            }
            animationCompleted += !proceduralMovement.DetectedMovementInput() ? Time.deltaTime * recoverySpeed : Time.deltaTime * speed;
        }
        else
        {
            //OPTIMIZATION OPPURTUNUITY - NO NEED TO UPDATE IT EVERY TIME
            //footRotator = Quaternion.FromToRotation(Vector3.up, newNormal) * Quaternion.FromToRotation(Vector3.forward, body.forward);
            //transform.rotation = footRotator * defaultToeRotation;

            if (FootMoved()) otherFoot.previouslyMoved = false;
            helperOldPos = helperNewPos;
            oldPosition = newPosition;
            oldNormal = newNormal;
        }
    }

    public bool MovingUp()
    {
        if (startingLeg)
        {
            //Debug.Log($"HNP, OHNP: {helperNewPos.y}, {otherFoot.helperNewPos.y}");
        }
        return previouslyMoved ? (helperNewPos.y - otherFoot.helperNewPos.y >= 0.01f) : (helperNewPos.y - otherFoot.helperNewPos.y <= 0.01f);
    }

    public bool MovingDown()
    {
        return !MovingUp();
    }

    private float CalculateIKChainLength()
    {
        float length = 0f;
        for (int i = 0; i < bones.Length - 1; i++)
        {
            length += Vector3.Distance(bones[i].position, bones[i + 1].position);
        }
        return length;
    }
    private void AnimateStep()
    {
        currentPosition = Vector3.Lerp(helperOldPos, helperNewPos, animationCompleted);
        //currentPosition = Vector3.Lerp(oldPosition, newPosition, animationCompleted);
        currentPosition.y += Mathf.Sin(animationCompleted * Mathf.PI) * stepHeight;
        currentNormal = Vector3.Lerp(oldNormal, newNormal, animationCompleted);
    }
    private void AnimateFootRotation()
    {
        footRotator = Quaternion.FromToRotation(Vector3.up, currentNormal) * Quaternion.FromToRotation(Vector3.forward, body.forward);
        transform.rotation = footRotator * defaultToeRotation;
    }
    private void FindHit(Vector3 rayOrigin)
    {
        Physics.SphereCast(rayOrigin, sphereCastRadius, Vector3.down, out hit, 1.5f, terrainLayer.value);
    }

    private void UpdateBodyAlignedHit()
    {
        if (!proceduralMovement.DetectedMovementInput()) return;
        Physics.SphereCast(GetStationaryFootRayCastPosition(), sphereCastRadius, Vector3.down, out bodyAlignedHit, 1.5f, terrainLayer.value);
    }
    private bool ShouldMove()
    {
        return proceduralMovement.DetectedMovementInput() &&
            !otherFoot.IsMoving() && animationCompleted >= 1f && !previouslyMoved &&
            (Vector3.Distance(bodyAlignedHit.point, currentPosition) > stepDistance);
    }
    public bool IsMoving()
    {
        return animationCompleted < 1;
    }

    private bool FootMoved()
    {
        return oldPosition != newPosition;
    }
    private Vector3 GetStationaryFootRayCastPosition()
    {
        return body.position + (body.right * footSideOffset) + /*Quaternion.FromToRotation(Vector3.up, newNormal) **/ Vector3.up * kneeHeightOffset;
    }

    private Vector3 GetMovingFootRayCastPosition(float distanceLeft)
    {
        return GetStationaryFootRayCastPosition() + (proceduralMovement.GetMovementSpeed() / speed + distanceLeft) * body.forward;
        //return GetStationaryFootRayCastPosition() + (proceduralMovement.GetScaledMovementSpeed()/speed + distanceLeft) * body.forward;
    }

    private Vector3 RotateAround(Vector3 objectPosition, Vector3 pivot, Quaternion rotation)
    {
        return rotation * (objectPosition - pivot) + pivot;
    }

    private void RecoverAnimation()
    {

    }
    private bool FindRecoveryHit()
    {
        CalculatePlaneParameters(body.right, Vector3.zero);
        Quaternion rotationToPlane = Quaternion.FromToRotation(Vector3.right, intersectionPlane.normal);
        Quaternion rotationToSlope = Quaternion.FromToRotation(Vector3.up, newNormal);
        //Vector3 line = footTransform.position - 5 * newNormal;
        //Vector3 intersectionPoint = CalculateIntersection(line);
        //recoveryHit.point = intersectionPoint;
        //return true;
        float angle = 360f;
        bool hitFound = false;
        //float distanceIncrement = 0;
        //Debug.Log("FINDING RECOVERY");
        //if distance increment gets bigger than stepLength (max distance that foot can be away from body), then hit obviously won't be found
        int increment = 1;

        float footPlaneAngle = GetFootAngleInPlane();
        //currently active foot must move backwards, static foot must move forwards
        //360 is forward, 180 is backward
        increment = previouslyMoved == true ? -1 : 1;

        int bound = previouslyMoved == true ? 180 : 360;
        angle = footPlaneAngle;
        //1ST CHECK IF FOOT IS ALREADY GROUNDED => NO NEED TO GO THROUGH WHILE LOOP IF IT IS
        //[footAngle, 360] for static foot, [180, footAngle] for moving foot
        //condition must work for both cases => [180, footAngle] is made into [-footAngle, -180] => this way condition works for both legs
        while (angle * increment <= bound * increment)
        {
            Vector3 pointOnPlane = CalculatePointOnPlane(angle);
            pointOnPlane = rotationToPlane * pointOnPlane;
            pointOnPlane = rotationToSlope * pointOnPlane;
            pointOnPlane += body.position + newNormal * chainLength;
            //Vector3 transformBackup = transform.position;
            //Physics.Raycast((footTransform.position - ((footTransform.position - body.position).normalized * distanceIncrement)), Vector3.down, out recoveryHit, 1.5f, terrainLayer.value);
            Physics.Raycast(pointOnPlane, Vector3.down, out recoveryHit, footHeightOffset, terrainLayer.value);
            //transform.position = transformBackup;
            if (recoveryHit.point != Vector3.zero)
            {
                Debug.Log($"ANGLE: {angle}");
                break;
            }
            //if (FootGrounded()) hitFound = true;
            else angle += increment;
        }
        //Debug.Log("FINISHED RECOVERY");
        //hit hasn't been found
        if (!hitFound) return false;
        else return true;
    }

    private Vector3 CalculatePointOnPlane(float angle)
    {
        angle = Mathf.Deg2Rad * angle;
        float y = chainLength * Mathf.Sin(angle);
        float z = chainLength * Mathf.Cos(angle);
        float x = (-intersectionPlane.normal.y * y + intersectionPlane.normal.z * z - intersectionPlane.D) / intersectionPlane.normal.x;
        return new Vector3(0, y, z);
    }
    private void CalculatePlaneParameters(Vector3 normal, Vector3 point)
    {
        intersectionPlane.normal = normal;
        intersectionPlane.D = -Vector3.Dot(normal, point);
    }
    private Vector3 CalculateIntersection(Vector3 line)
    {
        float A, B, C;
        A = line.x / intersectionPlane.normal.x;
        B = line.y / intersectionPlane.normal.y;
        C = line.z / intersectionPlane.normal.z;
        return new Vector3(A, B, C);
    }

    private float GetFootAngleInPlane()
    {
        return Mathf.Atan2(transform.position.y, transform.position.z) * Mathf.Rad2Deg;
    }
    private bool FootGrounded()
    {
        transform.position = recoveryHit.point;
        //Debug.Log($"DISTANCE: {Vector3.Distance(footTransform.position, recoveryHit.point) <= footHeightOffset}");
        return Vector3.Distance(footTransform.position, recoveryHit.point) <= footHeightOffset;
    }
    public float GetTargetHeight()
    {
        //helperNewPos.y works for walking down the slope, currentPosition.y works for walking up the obstacles
        //return Mathf.Min(currentPosition.y, bodyAlignedHit.point.y);
        return Mathf.Min(currentPosition.y, helperNewPos.y);
    }

    private void OnEnable()
    {
        proceduralMovement.OnMovementStopped += HandleMovementStopped;
        proceduralMovement.OnMovementStarted += HandleMovementStarted;
    }
    private void OnDisable()
    {
        proceduralMovement.OnMovementStopped -= HandleMovementStopped;
        proceduralMovement.OnMovementStarted -= HandleMovementStarted;
    }
    private void HandleMovementStopped()
    {
        FindRecoveryHit();
        Debug.Log($"HIT POS: {recoveryHit.point}");
        //Debug.Log("Movement stopped");
    }
    private void HandleMovementStarted()
    {
        //Physics.Raycast(new Vector3(0f, 20f, 0f), Vector3.up, out recoveryHit, footHeightOffset, terrainLayer.value);
        recoveryHit = new RaycastHit();
        //Debug.Log("Movement started");
    }
    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.red;

        //Gizmos.DrawCube(helperNewPos, new Vector3(0.075f, 0.075f, 0.075f));

        //Gizmos.color = Color.blue;

        //Gizmos.DrawCube(helperOldPos, new Vector3(0.075f, 0.075f, 0.075f));

        //Gizmos.color = movementBoxColor;
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(recoveryHit.point, sphereCastRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(footTransform.position, sphereCastRadius);


        CalculatePlaneParameters(body.right, Vector3.zero);
        Quaternion rotationToPlane = Quaternion.FromToRotation(Vector3.right, intersectionPlane.normal);
        Quaternion rotationToSlope = Quaternion.FromToRotation(Vector3.up, newNormal);
        for (int i = 360; i >= 180; i--)
        {
            Gizmos.color = Color.black;
            Vector3 pointOnPlane = CalculatePointOnPlane(i);
            // Rotate the point to align with the plane and move to world position
            pointOnPlane = rotationToPlane * pointOnPlane;
            pointOnPlane = rotationToSlope * pointOnPlane;
            pointOnPlane += body.position + newNormal * chainLength;
            Gizmos.DrawCube(pointOnPlane, new Vector3(sphereCastRadius, sphereCastRadius, sphereCastRadius));
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pointOnPlane, pointOnPlane + Vector3.down * footHeightOffset);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(helperOldPos, helperNewPos);
        Gizmos.DrawSphere(currentPosition, sphereCastRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(helperNewPos, sphereCastRadius);
    }
}