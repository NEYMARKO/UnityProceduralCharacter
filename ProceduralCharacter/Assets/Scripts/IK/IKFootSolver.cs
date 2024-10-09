using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineInternal;

public class IKFootSolver : MonoBehaviour
{
    [SerializeField] LayerMask terrainLayer;
    [SerializeField] ProceduralMovement proceduralMovement;
    [SerializeField] float speed = 1f;
    [SerializeField] float stepDistance = 1f;
    [SerializeField] float stepLength = 1f;
    [SerializeField] float stepHeight = 1f;
    [SerializeField] float footHeightOffset = 0.1f;
    //[SerializeField] float footForwardOffset = 0.2f;
    //[SerializeField] Vector3 footOffset;
    [SerializeField] IKFootSolver otherFoot;
    [SerializeField] Transform toeBase;
    //[SerializeField] Transform toeEnd;
    //foot side offset from the center of the body
    float footSpacing;
    float sphereCastRadius = 0.1f;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    Vector3 kneeHeight;
    Transform body;
    float lerp;
    RaycastHit hit;
    Quaternion oldToeRotation;
    // Start is called before the first frame update
    void Start()
    {
        footSpacing = transform.localPosition.x;
        currentPosition = oldPosition = newPosition = transform.position;
        currentNormal = oldNormal = newNormal = transform.up;
        kneeHeight = new Vector3(0f, 0.5f, 0f);
        body = proceduralMovement.transform;
        oldToeRotation = toeBase.rotation;
        lerp = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        //if (!proceduralMovement.CharacterMoving())
        //{
        //    lerp = 1f;
        //    FindHit(body.position + (body.right * footSpacing) + kneeHeight);
        //    currentPosition = hit.point;
        //    currentNormal = hit.normal;
        //}
        transform.position = currentPosition + currentNormal * footHeightOffset;
        //transform.up = currentNormal;
        //because orientation of bones is weird - it's z is pointing up
        //SINCE IT'S FORWARD ISN'T DIRECTLY LOOKING UP, THERE SHOULD BE SOME TWEAKS - ROTATE IT'S FORWARD FOR THE ANGLE
        //BETWEEN NEW AND OLD NORMAL
        Quaternion footRotation = Quaternion.FromToRotation(oldNormal, newNormal);
        toeBase.up = oldToeRotation * currentNormal;
        FindHit(body.position + (body.right * footSpacing) + kneeHeight);
        if (HitFound())
        {
            if (ShouldMove())
            {
                lerp = 0f;
                FindHit(hit.point + kneeHeight + (proceduralMovement.GetMovementSpeed() / speed + stepLength) * body.forward);
                newPosition = hit.point;
                newNormal = hit.normal;
                oldToeRotation = toeBase.rotation;
            }
            //newPosition = hit.point;
            //oldPosition = currentPosition;
        }
        if (lerp < 1f)
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
        Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, lerp);
        tempPosition.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;

        currentPosition = tempPosition;
        currentNormal = Vector3.Lerp(oldNormal, newNormal, lerp);
        lerp += Time.deltaTime * speed;
    }
    private void FindHit(Vector3 rayOrigin)
    {
        //RaycastHit hit;
        Physics.SphereCast(rayOrigin, sphereCastRadius, Vector3.down, out hit, 1.5f, terrainLayer.value);
    }

    private bool ShouldMove()
    {
        return (Vector3.Distance(hit.point, currentPosition) > stepDistance) && !otherFoot.IsMoving() && lerp >= 1f;
    }
    
    private bool HitFound()
    {
        return hit.point != Vector3.zero;
    }

    public bool IsMoving()
    {
        return lerp < 1;
    }

    private void OnDrawGizmos()
    {

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition, 0.1f);
    }
}
