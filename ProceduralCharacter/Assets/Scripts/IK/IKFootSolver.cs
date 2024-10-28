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
    //[SerializeField] Transform foot;
    [SerializeField] float footOffsetTolerance;
    //foot side offset from the center of the body
    float footSpacing;
    float sphereCastRadius = 0.1f;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    [SerializeField] Vector3 kneeHeight;
    Transform body;
    float animationCompleted;
    bool previouslyMoved = false;
    RaycastHit hit, bodyAlignedHit;
    Quaternion oldToeRotation;
    
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
        currentNormal = oldNormal = newNormal = transform.up;
        body.position += body.forward * stepLength / 2;
    }

    void Update()
    {
        transform.position = currentPosition + currentNormal * footHeightOffset;
        //transform.up = currentNormal;
        //because orientation of bones is weird - it's z is pointing up
        //SINCE IT'S FORWARD ISN'T DIRECTLY LOOKING UP, THERE SHOULD BE SOME TWEAKS - ROTATE IT'S FORWARD FOR THE ANGLE
        //BETWEEN NEW AND OLD NORMAL
        Quaternion footRotation = Quaternion.FromToRotation(oldNormal, newNormal);
        footTransform.up = oldToeRotation * currentNormal;
        UpdateBodyAlignedHit();
        if (ShouldMove())
        {
            animationCompleted = 0f;
            FindHit(GetMovingFootRayCastPosition());
            newPosition = hit.point;
            newNormal = hit.normal;
            oldToeRotation = footTransform.rotation;
            previouslyMoved = true;
        }
        if (animationCompleted < 1f)
        {
            AnimateStep();
            animationCompleted += Time.deltaTime * speed;
        }
        else
        {
            otherFoot.previouslyMoved = false;
            oldPosition = newPosition;
            oldNormal = newNormal;
            //Quaternion footRotator = Quaternion.FromToRotation(transform.forward, newNormal);

            //transform.forward = oldToeRotation * newNormal;
        }
    }
    
    private void AnimateStep()
    {
        Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, animationCompleted);
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
        return body.position + (body.right * footSpacing) + kneeHeight;
    }

    private Vector3 GetMovingFootRayCastPosition()
    {
        return GetStationaryFootRayCastPosition() + (proceduralMovement.GetMovementSpeed()/speed + stepLength) * body.forward;
    }
    
    public float GetTargetHeight()
    {
        //if (IsMoving()) return float.MinValue;
        return newPosition.y;
    }
    private void OnDrawGizmos()
    {
        if (Vector3.Distance(bodyAlignedHit.point, hit.point) >= stepDistance)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(bodyAlignedHit.point, hit.point);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(bodyAlignedHit.point, 0.1f);
    }
}
