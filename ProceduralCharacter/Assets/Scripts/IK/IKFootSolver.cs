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
    [SerializeField] float speed = 1f;
    [SerializeField] float stepDistance = 1f;
    [SerializeField] float stepLength = 1f;
    [SerializeField] float stepHeight = 1f;
    [SerializeField] float footHeightOffset = 0.1f;
    [SerializeField] Vector3 kneePosition;
    [SerializeField] Transform footTransform;
    [SerializeField] float angleOffsetTolerance;
    //foot side offset from the center of the body
    float footSpacing;
    float sphereCastRadius = 0.05f;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    Vector3 enteringBodyForward = Vector3.zero;
    Transform body;
    float animationCompleted;
    bool previouslyMoved = false;
    RaycastHit hit, bodyAlignedHit, recoveryHit;
    Quaternion defaultToeRotation, footRotator = Quaternion.identity;
    Quaternion lastPlayerRotation;
    [Header("Recovery")]
    [SerializeField] float recoverySpeed;
    Vector3 enteringForward;
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
        //Debug.Log("CHAIN LENGTH: " + chainLength);
        //STARTING POSITION
        if (startingLeg)
        {
            transform.position = body.position + body.right * footSpacing - body.forward * stepLength;
        }
        else
        {
            transform.position = body.position + body.right * footSpacing + body.forward * stepLength;
            previouslyMoved = true;
        }
        
        currentPosition = oldPosition = newPosition = transform.position;
        currentNormal = oldNormal = newNormal = transform.up;
        animationCompleted = 1f;

        defaultToeRotation = footTransform.rotation;
        footRotator = Quaternion.FromToRotation(Vector3.up, body.up) * Quaternion.FromToRotation(Vector3.forward, body.forward);
        lastPlayerRotation = proceduralMovement.GetPlayerRotation();
    }

    void Update()
    {
        //transform.position = !proceduralMovement.DetectedMovementInput() ? recoveryHit.point : currentPosition + newNormal * footHeightOffset;
        transform.position = currentPosition + footRotator * Vector3.up * footHeightOffset;
        UpdateBodyAlignedHit();
        
        if (ShouldMove())
        {
            previouslyMoved = true;
            animationCompleted = 0f;
            FindHit(GetMovingFootRayCastPosition(stepLength));
            enteringForward = body.forward;
            newPosition = hit.point;
            newNormal = hit.normal;
            enteringBodyForward = body.forward;
        }
        if (animationCompleted < 1f)
        {
            //if (!proceduralMovement.DetectedMovementInput())
            //{
            //    if(FindRecoveryHit())
            //    {
            //        transform.position = recoveryHit.point;
            //        //animationCompleted = 0f;
            //        //newPosition = recoveryHit.point;
            //    }
            //}
            //else recoveryHit.point = Vector3.zero;
            AnimateStep();
            animationCompleted += !proceduralMovement.DetectedMovementInput() ? Time.deltaTime * recoverySpeed : Time.deltaTime * speed;
        }
        else
        {
            //OPTIMIZATION OPPURTUNUITY - NO NEED TO UPDATE IT EVERY TIME
            footRotator = Quaternion.FromToRotation(Vector3.up, newNormal) * Quaternion.FromToRotation(Vector3.forward, body.forward);
            transform.rotation = footRotator * defaultToeRotation;

            ////if body has changed it's forward vector (for example if you are heading up the slope, your feet will get
            ////rotated in some way, but if you are coming down that slope again, you would want them to be rotated differently
            ////slope will have different normal, but you will be heading in the opposite direction
            //if (oldNormal != newNormal || Vector3.Angle(enteringBodyForward, body.forward) >= 90f)
            //{
                
            //    //transform.rotation = Quaternion.LookRotation(body.forward, newNormal) * oldToeRotation;
            //}
            // leg has been moved - it can free other leg so it could move next
            if (FootMoved()) otherFoot.previouslyMoved = false;
            oldPosition = newPosition;
            oldNormal = newNormal;
        }
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
        currentPosition = Vector3.Lerp(oldPosition, newPosition, animationCompleted);
        currentPosition.y += Mathf.Sin(animationCompleted * Mathf.PI) * stepHeight;
        currentPosition = Quaternion.FromToRotation(enteringForward, body.forward) * (currentPosition - body.position) + body.position;
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
        return body.position + (body.right * footSpacing) + kneePosition;
    }

    private Vector3 GetMovingFootRayCastPosition(float distanceLeft)
    {
        return GetStationaryFootRayCastPosition() + (proceduralMovement.GetMovementSpeed()/speed + distanceLeft) * body.forward;
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
        return newPosition.y;
    }

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.red;

        //Gizmos.DrawCube(newPosition, new Vector3(sphereCastRadius, sphereCastRadius, sphereCastRadius));

        //Gizmos.color = Color.blue;

        //Gizmos.DrawCube(oldPosition, new Vector3(sphereCastRadius, sphereCastRadius, sphereCastRadius));
        //Gizmos.color = Color.green;
        //Gizmos.DrawLine(oldPosition, newPosition);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(recoveryHit.point, sphereCastRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(footTransform.position, sphereCastRadius);
        Gizmos.color = Color.black;

        CalculatePlaneParameters(body.right, Vector3.zero);
        Quaternion rotationToPlane = Quaternion.FromToRotation(Vector3.right, intersectionPlane.normal);
        Quaternion rotationToSlope = Quaternion.FromToRotation(Vector3.up, newNormal);
        for (int i = 360; i >= 180; i--)
        {
            Vector3 pointOnPlane = CalculatePointOnPlane(i);

            // Rotate the point to align with the plane and move to world position
            pointOnPlane = rotationToPlane * pointOnPlane;
            pointOnPlane = rotationToSlope * pointOnPlane;
            pointOnPlane += body.position + newNormal * chainLength;
            Gizmos.DrawCube(pointOnPlane, new Vector3(sphereCastRadius, sphereCastRadius, sphereCastRadius));
        }    
        
    }
}
