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
    [SerializeField] Vector3 footOffset;
    [SerializeField] IKFootSolver otherFoot;
    //foot side offset from the center of the body
    float footSpacing;
    float sphereCastRadius = 0.1f;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    float lerp;
    RaycastHit hit;
    // Start is called before the first frame update
    void Start()
    {
        footSpacing = transform.localPosition.x;
        currentPosition = oldPosition = newPosition = transform.position;
        currentNormal = oldNormal = newNormal = transform.up;
        lerp = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        //FindHit();
        //if (ShouldMove())
        //{
        //    lerp = 0f;
        //    if (HitFound())
        //    {
        //        transform.position = hit.point;
        //    }
        //    //newPosition = hit.point;
        //    //oldPosition = currentPosition;
        //}
        transform.position = currentPosition;
        transform.up = currentNormal;

        Ray ray = new Ray(body.position + (body.right * footSpacing), Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit info, 10, terrainLayer.value))
        {
            if (Vector3.Distance(newPosition, info.point) > stepDistance && !otherFoot.IsMoving() && lerp >= 1)
            {
                lerp = 0;
                int direction = body.InverseTransformPoint(info.point).z > body.InverseTransformPoint(newPosition).z ? 1 : -1;
                newPosition = info.point + (body.forward * stepLength * direction) + footOffset;
                newNormal = info.normal;
            }
        }

        if (lerp < 1)
        {
            Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, lerp);
            tempPosition.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;

            currentPosition = tempPosition;
            currentNormal = Vector3.Lerp(oldNormal, newNormal, lerp);
            lerp += Time.deltaTime * speed;
        }
        else
        {
            oldPosition = newPosition;
            oldNormal = newNormal;
        }
    }
    
    private void FindHit()
    {
        RaycastHit hit;
        Physics.SphereCast(body.position + (body.right * footSpacing) + new Vector3(0f, 0.5f, 0f), sphereCastRadius, Vector3.down, out hit, 1f, terrainLayer.value);
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
