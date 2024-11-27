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
    float footSpacing;
    float sphereCastRadius = 0.05f;
    public Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    Transform body;
    float animationCompleted;
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
    Vector3 helperHalfPoint;
    Vector3 _hp;
    float lineLength;
    Vector3 averageForward;
    float forwardAverageSmoothFactor = 2f;
    float positionSmoothFactor = 2f;

    private Vector3 initialFootOffset;
    private Vector3 localNewPosition, localOldPosition;

    struct Plane
    {
        public Vector3 normal;
        public float D;
    }
    Plane intersectionPlane;
    void Start()
    {
        footSpacing = transform.localPosition.x;
        body = proceduralMovement.transform;

        chainLength = CalculateIKChainLength();
        
        if (startingLeg)
        {
            //transform.position = body.position + body.right * footSpacing - body.forward * stepLength;
        }
        else
        {
            //transform.position = body.position + body.right * footSpacing + body.forward * stepLength;
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
        //transform.position = currentPosition + footRotator * Vector3.up * footHeightOffset;
        //transform.position = footTransform.position;
        UpdateBodyAlignedHit();

       
        //helperOldPos = bodyAlignedHit.point - body.forward * animationCompleted * (proceduralMovement.GetMovementSpeed() / speed + stepLength);

        //if (startingLeg)
        //{
        //    Debug.Log($"NEW POS: {newPosition}");
        //    Debug.Log($"OLD POS: {oldPosition}");
        //}
        //helperOldPos = oHit.point;
        //if (Vector3.Angle(previousForwardDirection, body.forward) > angleTolerance)
        //{
        //    helperNewPos = bodyAlignedHit.point + body.forward * (1 - animationCompleted) * (proceduralMovement.GetMovementSpeed() / speed + stepLength);
        //    helperOldPos = bodyAlignedHit.point - body.forward * animationCompleted * (proceduralMovement.GetMovementSpeed() / speed + stepLength);
        //}

        //averageForward = Vector3.Lerp(averageForward, body.forward, Time.deltaTime * forwardAverageSmoothFactor);

        //Vector3 targetHelperNewPos = bodyAlignedHit.point + averageForward * (1 - animationCompleted) * (proceduralMovement.GetMovementSpeed() / speed + stepLength);
        //Vector3 targetHelperOldPos = bodyAlignedHit.point - averageForward * animationCompleted * (proceduralMovement.GetMovementSpeed() / speed + stepLength);

        //helperNewPos = Vector3.Lerp(helperNewPos, targetHelperNewPos, Time.deltaTime * positionSmoothFactor);
        //helperOldPos = Vector3.Lerp(helperOldPos, targetHelperOldPos, Time.deltaTime * positionSmoothFactor);


        if (ShouldMove())
        {
            previouslyMoved = true;
            animationCompleted = 0f;
            previousForwardDirection = body.forward;
            otherFoot.previousForwardDirection = body.forward;
            oldBodyPos = body.position;
            otherFoot.oldBodyPos = oldBodyPos;
            FindHit(GetMovingFootRayCastPosition(stepLength));
            //oldPosition = Quaternion.FromToRotation(previousForwardDirection, body.forward) * (oldPosition - body.position) + body.position;
            newPosition = hit.point;
            newNormal = hit.normal;
            helperNewPos = bodyAlignedHit.point + body.forward * (1 - animationCompleted) * (proceduralMovement.GetMovementSpeed() / speed + stepLength);
            //helperOldPos = bodyAlignedHit.point - body.forward * animationCompleted * (proceduralMovement.GetMovementSpeed() / speed + stepLength);
            helperOldPos = oldPosition;
            //initialFootOffset = Quaternion.Inverse(body.rotation) * (newPosition - bodyAlignedHit.point);
            
            //IT IS SAVED RELATIVE TO THE BODY IN THE POINT IN TIME WHEN BODY HASN'T STARTED MOVING - IT WILL ALWAYS BE RIGHT NEXT TO BODY WHEN CONVERTED TO WORLD COORDINATES
            //IT SHOULD BE SAVED RELATIVE TO NEW POSITION - THERE NEEDS TO BE TRANSFORM THAT HAS POSITION OF NEWPOSITION
            localOldPosition = body.InverseTransformPoint(helperOldPos);

        }
        if (animationCompleted < 1f && previouslyMoved)
        {

            RaycastHit nHit;
            RaycastHit oHit;
            //Physics.SphereCast(bodyAlignedHit.point + Vector3.up * kneeHeightOffset + body.forward * (1 - Mathf.Clamp(animationCompleted, 0f, 1f)) * (proceduralMovement.GetMovementSpeed() / speed + stepLength)
            //    , sphereCastRadius, Vector3.down, out nHit, 1.5f, terrainLayer.value);
            Physics.SphereCast(oldBodyPos + body.right * footSpacing + Vector3.up * kneeHeightOffset + body.forward * (proceduralMovement.GetMovementSpeed() / speed + stepLength)
                , sphereCastRadius, Vector3.down, out nHit, 1.5f, terrainLayer.value);
            //Physics.SphereCast(body.TransformPoint(localOldPosition) + Vector3.up * kneeHeightOffset, sphereCastRadius, Vector3.down, out oHit, 1.5f, terrainLayer.value);
            //Physics.SphereCast(bodyAlignedHit.point + Vector3.up * kneeHeightOffset - body.forward * animationCompleted * (proceduralMovement.GetMovementSpeed() / speed + stepLength)
            //    , sphereCastRadius, Vector3.down, out oHit, 1.5f, terrainLayer.value);
            helperNewPos = nHit.point;
            helperOldPos = body.TransformPoint(localOldPosition);
            //helperOldPos = helperNewPos - body.forward * (proceduralMovement.GetMovementSpeed() / speed + stepLength);
            //helperOldPos = oldBodyPos + body.right * footSpacing;
            //Physics.SphereCast(body.TransformPoint(localNewPosition) + Vector3.up * kneeHeightOffset, sphereCastRadius, Vector3.down, out nHit, 1.5f, terrainLayer.value);

            //Physics.SphereCast(bodyAlignedHit.point + body.rotation * initialFootOffset + Vector3.up * kneeHeightOffset, sphereCastRadius, Vector3.down, out nHit, 1.5f, terrainLayer.value);

            //newPosition = body.position + body.rotation * initialFootOffset;
            //newPosition = nHit.point;
            AnimateStep();
            animationCompleted += !proceduralMovement.DetectedMovementInput() ? Time.deltaTime * recoverySpeed : Time.deltaTime * speed;
            //helperHalfPoint = proceduralMovement.transform.position + proceduralMovement.transform.forward * 
            //    (proceduralMovement.GetMovementSpeed() / speed + stepLength) * animationCompleted
            //    + body.right * footSpacing;
        }
        else
        {
            //OPTIMIZATION OPPURTUNUITY - NO NEED TO UPDATE IT EVERY TIME
            footRotator = Quaternion.FromToRotation(Vector3.up, newNormal) * Quaternion.FromToRotation(Vector3.forward, body.forward);
            transform.rotation = footRotator * defaultToeRotation;

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
        return body.position + (body.right * footSpacing) + /*Quaternion.FromToRotation(Vector3.up, newNormal) **/ Vector3.up * kneeHeightOffset;
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
        CalculatePlaneParameters(body.right, footTransform.position);

        //Vector3 line = footTransform.position - 5 * newNormal;
        //Vector3 intersectionPoint = CalculateIntersection(line);
        //recoveryHit.point = intersectionPoint;
        //return true;
        float angle = 360f;
        bool hitFound = false;
        //float distanceIncrement = 0;
        Debug.Log("FINDING RECOVERY");
        //if distance increment gets bigger than stepLength (max distance that foot can be away from body), then hit obviously won't be found




        //1ST CHECK IF FOOT IS ALREADY GROUNDED => NO NEED TO GO THROUGH WHILE LOOP IF IT IS
        while (!hitFound && angle >= 180f)
        {
            Vector3 pointOnPlane = CalculatePointOnPlane(angle);
            //Vector3 transformBackup = transform.position;
            //Physics.Raycast((footTransform.position - ((footTransform.position - body.position).normalized * distanceIncrement)), Vector3.down, out recoveryHit, 1.5f, terrainLayer.value);
            Physics.Raycast(pointOnPlane, -newNormal, out recoveryHit, footHeightOffset, terrainLayer.value);
            //transform.position = transformBackup;
            if (recoveryHit.point != Vector3.zero) hitFound = true;
            //if (FootGrounded()) hitFound = true;
            else angle -= 1f;
        }
        Debug.Log("FINISHED RECOVERY");
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
        A = line.x/intersectionPlane.normal.x;
        B = line.y/intersectionPlane.normal.y;
        C = line.z/intersectionPlane.normal.z;
        return new Vector3(A, B, C);
    }
    private bool FootGrounded()
    {
        transform.position = recoveryHit.point;
        //Debug.Log($"DISTANCE: {Vector3.Distance(footTransform.position, recoveryHit.point) <= footHeightOffset}");
        return Vector3.Distance(footTransform.position, recoveryHit.point) <= footHeightOffset;
    }
    public float GetTargetHeight()
    {
        return currentPosition.y;
        //return newPosition.y;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawCube(helperNewPos, new Vector3(0.075f, 0.075f, 0.075f));
        //Gizmos.DrawCube(newPosition, new Vector3(0.2f, 0.2f, 0.2f));

        Gizmos.color = Color.blue;

        Gizmos.DrawCube(helperOldPos, new Vector3(0.075f, 0.075f, 0.075f));
        //Gizmos.DrawCube(oldPosition, new Vector3(0.2f, 0.2f, 0.2f));
        //Gizmos.color = Color.green;
        //Gizmos.DrawLine(oldPosition, newPosition);

        //Gizmos.color = Color.magenta;
        //Gizmos.DrawSphere(recoveryHit.point, sphereCastRadius);
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawSphere(footTransform.position, sphereCastRadius);
        //Gizmos.color = Color.black;

        //CalculatePlaneParameters(body.right, Vector3.zero);
        //Quaternion rotationToPlane = Quaternion.FromToRotation(Vector3.right, intersectionPlane.normal);
        //Quaternion rotationToSlope = Quaternion.FromToRotation(Vector3.up, newNormal);
        //for (int i = 360; i >= 180; i--)
        //{
        //    Vector3 pointOnPlane = CalculatePointOnPlane(i);

        //    // Rotate the point to align with the plane and move to world position
        //    pointOnPlane = rotationToPlane * pointOnPlane;
        //    pointOnPlane = rotationToSlope * pointOnPlane;
        //    pointOnPlane += body.position + newNormal * chainLength;
        //    Gizmos.DrawCube(pointOnPlane, new Vector3(sphereCastRadius, sphereCastRadius, sphereCastRadius));
        //}

        //if (startingLeg)
        //{
        //    //movementBoxDimensions =  new Vector3(footSpacing * 2, 0f, proceduralMovement.GetMovementMagnitude() + stepLength);
        //    Gizmos.matrix = Matrix4x4.TRS(proceduralMovement.transform.position + new Vector3(0f, 0.05f, 0f) /*+ proceduralMovement.transform.forward * (proceduralMovement.GetMovementSpeed()/speed + stepLength)*/,
        //        Quaternion.identity, new Vector3(footSpacing * 2, 0f, proceduralMovement.GetMovementMagnitude() + stepLength));
        //    Gizmos.color = movementBoxColor;
        //    Gizmos.DrawCube(Vector3.zero + proceduralMovement.transform.forward * (proceduralMovement.GetMovementMagnitude() + stepLength) / 2, new Vector3(1f, 1f, 1f));
        //    Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
        //    Gizmos.color = Color.red;
        //    Gizmos.DrawLine(proceduralMovement.transform.position, proceduralMovement.transform.position + proceduralMovement.transform.forward * 5f);
        //}

        //Quaternion rotation = Quaternion.FromToRotation(previousForwardDirection, proceduralMovement.transform.forward);
        //_hp = rotation * (helperHalfPoint - oldBodyPos) + proceduralMovement.transform.position;
        Gizmos.color = movementBoxColor;
        Gizmos.DrawCube(helperHalfPoint, new Vector3(0.1f, 0.1f, 0.1f));
        //helperNewPos = GetStationaryFootRayCastPosition() + body.forward * (1 - animationCompleted) * (proceduralMovement.GetMovementSpeed()/speed + stepLength);
        //helperOldPos = GetStationaryFootRayCastPosition() - body.forward * animationCompleted * (proceduralMovement.GetMovementSpeed()/speed + stepLength);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(helperOldPos, helperNewPos);
        Gizmos.DrawSphere(currentPosition, sphereCastRadius);
        
    }
}
