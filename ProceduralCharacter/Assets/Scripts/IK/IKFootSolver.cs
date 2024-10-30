using System.Collections;
using System.Collections.Generic;
using System.Security;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngineInternal;

public class IKFootSolver : MonoBehaviour
{
    [SerializeField] LayerMask terrainLayer;
    [SerializeField] ProceduralMovement proceduralMovement;
    [SerializeField] bool startingLeg = false;
    [SerializeField] float speed = 1f;
    [SerializeField] float stepDistance = 1f;
    [SerializeField] float stepLength = 1f;
    [SerializeField] float stepHeight = 1f;
    [SerializeField] float footHeightOffset = 0.1f;
    [SerializeField] IKFootSolver otherFoot;
    [SerializeField] Transform footTransform;
    [SerializeField] float angleOffsetTolerance;
    //foot side offset from the center of the body
    float footSpacing;
    float sphereCastRadius = 0.1f;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    [SerializeField] Vector3 kneePosition;
    Transform body;
    float animationCompleted;
    bool previouslyMoved = false;
    RaycastHit hit, bodyAlignedHit;
    Quaternion oldToeRotation;
    Quaternion lastPlayerRotation;
    void Start()
    {
        footSpacing = transform.localPosition.x;
        //kneeHeight.y = hipsHeight / 2;
        body = proceduralMovement.transform;
        oldToeRotation = footTransform.rotation;
        animationCompleted = 1f;
        if (startingLeg) transform.position = body.position + body.right * footSpacing + body.forward * stepLength * 2;
        else previouslyMoved = true;
        currentPosition = oldPosition = newPosition = transform.position;
        //currentPosition = oldPosition = newPosition = body.InverseTransformPoint(transform.position);
        currentNormal = oldNormal = newNormal = transform.up;
        body.position += body.forward * stepLength / 2;
        lastPlayerRotation = proceduralMovement.GetPlayerRotation();
    }

    void Update()
    {
        transform.position = currentPosition + newNormal * footHeightOffset;
        UpdateBodyAlignedHit();
        
        if (ShouldMove())
        {
            animationCompleted = 0f;
            FindHit(GetMovingFootRayCastPosition(stepLength));
            newPosition = hit.point;
            //newPosition = body.InverseTransformPoint(hit.point);
            newNormal = hit.normal;
            previouslyMoved = true;
        }
        if (animationCompleted < 1f)
        {
            
            //if (Quaternion.Angle(lastPlayerRotation, proceduralMovement.GetPlayerRotation()) >= angleOffsetTolerance)
            //{
            //    lastPlayerRotation = proceduralMovement.GetPlayerRotation();
                
            //    //FindHit(GetMovingFootRayCastPosition(stepLength * (1 - animationCompleted)));
            //    //newPosition = hit.point;
            //    //newNormal = hit.normal;
            //    //oldPosition = currentPosition;
            //    //oldNormal = currentNormal;
            //}
            AnimateStep();
            animationCompleted += Time.deltaTime * speed;
        }
        else
        {
            if (oldNormal != newNormal)
            {
                transform.rotation = Quaternion.LookRotation(body.forward, newNormal) * oldToeRotation;
            }
            // leg has been moved
            if (oldPosition != newPosition) otherFoot.previouslyMoved = false;
            oldPosition = newPosition;
            //oldPosition = body.InverseTransformPoint(newPosition);
            oldNormal = newNormal;
        }
    }
    
    private void AnimateStep()
    {
        Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, animationCompleted);

        //tempPosition = body.TransformPoint(tempPosition);
        tempPosition.y += Mathf.Sin(animationCompleted * Mathf.PI) * stepHeight;

        currentPosition = tempPosition;
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
        Debug.Log($"{(startingLeg ? "RIGHT" : "LEFT")} LEG previouslyMoved: {previouslyMoved}");
        return proceduralMovement.DetectedMovementInput() &&
            !otherFoot.IsMoving() && animationCompleted >= 1f && !previouslyMoved &&
            (Vector3.Distance(bodyAlignedHit.point, currentPosition) > stepDistance);
    }
    public bool IsMoving()
    {
        return animationCompleted < 1;
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
    
    public float GetTargetHeight()
    {
        //if (IsMoving()) return float.MinValue;
        return newPosition.y;
    }
    private void OnDrawGizmos()
    {
        //if (Vector3.Distance(bodyAlignedHit.point, hit.point) >= stepDistance)
        //{
        //    Gizmos.color = Color.green;
        //    Gizmos.DrawLine(bodyAlignedHit.point, hit.point);
        //}

        Gizmos.color = Color.red;
        //Gizmos.DrawSphere(newPosition, 0.1f);

        Gizmos.DrawCube(newPosition, new Vector3(sphereCastRadius, sphereCastRadius, sphereCastRadius));

        Gizmos.color = Color.blue;
        //Gizmos.DrawSphere(newPosition, 0.1f);

        Gizmos.DrawCube(oldPosition, new Vector3(sphereCastRadius, sphereCastRadius, sphereCastRadius));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(oldPosition, newPosition);
        //Gizmos.color = Color.blue;
        //Gizmos.DrawSphere(bodyAlignedHit.point, 0.1f);
    }
}
