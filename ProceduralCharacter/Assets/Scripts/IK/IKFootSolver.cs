using System.Collections;
using System.Collections.Generic;
using System.Security;
using TreeEditor;
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
    [SerializeField] Transform toeBase;
    //foot side offset from the center of the body
    float footSpacing;
    float sphereCastRadius = 0.1f;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    [SerializeField] Vector3 kneeHeight;
    Transform body;
    float animationCompleted;
    RaycastHit hit, bodyAlignedHit;
    Quaternion oldToeRotation;
    
    // Start is called before the first frame update
    void Start()
    {
        footSpacing = transform.localPosition.x;
        //kneeHeight = new Vector3(0f, 0.5f, 0f);
        body = proceduralMovement.transform;
        oldToeRotation = toeBase.rotation;
        animationCompleted = 1f;
        if (startingLeg) transform.position = body.position + body.right * footSpacing + body.forward * stepLength * 2;
        currentPosition = oldPosition = newPosition = transform.position;
        currentNormal = oldNormal = newNormal = transform.up;
        body.position += body.forward * stepLength / 2;
    }

    // Update is called once per frame
    void Update()
    {
        //Only if target has been reached can it move again
        if (kneeHeight == Vector3.zero) return;

        transform.position = currentPosition + currentNormal * footHeightOffset;
        //transform.up = currentNormal;
        //because orientation of bones is weird - it's z is pointing up
        //SINCE IT'S FORWARD ISN'T DIRECTLY LOOKING UP, THERE SHOULD BE SOME TWEAKS - ROTATE IT'S FORWARD FOR THE ANGLE
        //BETWEEN NEW AND OLD NORMAL
        Quaternion footRotation = Quaternion.FromToRotation(oldNormal, newNormal);
        toeBase.up = oldToeRotation * currentNormal;
        UpdateBodyAlignedHit();
        if (ShouldMove())
        {
            animationCompleted = 0f;
            FindHit(GetMovingFootRayCastPosition());
            newPosition = hit.point;
            newNormal = hit.normal;
            oldToeRotation = toeBase.rotation;
        }
        //newPosition = hit.point;
        //oldPosition = currentPosition;
        if (animationCompleted < 1f)
        {
            //if (proceduralMovement.CharacterMoving()) AnimateStep();
            AnimateStep();
            //lerp += speed * Time.deltaTime;
        }
        else
        {
            oldPosition = newPosition;
            oldNormal = newNormal;
        }
    }
    
    private void AnimateStep()
    {
        Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, animationCompleted);
        tempPosition.y += Mathf.Sin(animationCompleted * Mathf.PI) * stepHeight;

        currentPosition = tempPosition;
        currentNormal = Vector3.Lerp(oldNormal, newNormal, animationCompleted);
        animationCompleted += Time.deltaTime * speed;
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
        return (Vector3.Distance(bodyAlignedHit.point, currentPosition) > stepDistance) && !otherFoot.IsMoving() && animationCompleted >= 1f && proceduralMovement.DetectedMovementInput();
    }
    
    private bool HitFound()
    {
        return hit.point != Vector3.zero;
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
        return GetStationaryFootRayCastPosition() + (proceduralMovement.GetScaledMovementSpeed() / speed + stepLength) * body.forward;
    }
    private bool BodyStopped()
    {
        return !proceduralMovement.DetectedMovementInput();
    }
    
    private void OnDrawGizmos()
    {

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(bodyAlignedHit.point, 0.1f);
    }
}
