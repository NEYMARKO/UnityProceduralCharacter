using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineInternal;

public class IKFootSolver : MonoBehaviour
{
    [SerializeField] LayerMask terrainLayer;
    [SerializeField] Transform body;
    [SerializeField] float speed = 1f;
    [SerializeField] float stepDistance = 1f;
    [SerializeField] float stepLength = 1f;
    [SerializeField] float stepHeight = 1f;
    [SerializeField] float footHeightOffset = 0.1f;
    [SerializeField] float footForwardOffset = 0.2f;
    [SerializeField] Vector3 footOffset;
    [SerializeField] IKFootSolver otherFoot;
    [SerializeField] Transform toeBase;
    //[SerializeField] Transform toeEnd;
    //foot side offset from the center of the body
    float footSpacing;
    float sphereCastRadius = 0.1f;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    Vector3 kneeHeight;
    float lerp;
    RaycastHit hit;
    // Start is called before the first frame update
    void Start()
    {
        footSpacing = transform.localPosition.x;
        currentPosition = oldPosition = newPosition = transform.position;
        currentNormal = oldNormal = newNormal = transform.up;
        kneeHeight = new Vector3(0f, 1f, 0f);
        lerp = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = currentPosition + currentNormal * footHeightOffset;
        //transform.up = currentNormal;
        //because orientation of bones is weird - it's z is pointing up
        //SINCE IT'S FORWARD ISN'T DIRECTLY LOOKING UP, THERE SHOULD BE SOME TWEAKS - ROTATE IT'S FORWARD FOR THE ANGLE
        //BETWEEN NEW AND OLD NORMAL
        Quaternion footRotation = Quaternion.FromToRotation(oldNormal, newNormal);
        toeBase.rotation = footRotation * toeBase.rotation;
        FindHit();
        if (HitFound())
        {
            if (ShouldMove())
            {
                lerp = 0f;
                newPosition = hit.point;
                newNormal = hit.normal;
            }
            //newPosition = hit.point;
            //oldPosition = currentPosition;
        }
        if (lerp < 1f)
        {
            AnimateStep();
            lerp += speed * Time.deltaTime;
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
    private void FindHit()
    {
        //RaycastHit hit;
        Physics.SphereCast(body.position + (body.right * footSpacing) + kneeHeight, sphereCastRadius, Vector3.down, out hit, 1.5f, terrainLayer.value);
    }

    private bool ShouldMove()
    {
        return (Vector3.Distance(hit.point, currentPosition) >= stepDistance) && !otherFoot.IsMoving() && lerp >= 1f;
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
