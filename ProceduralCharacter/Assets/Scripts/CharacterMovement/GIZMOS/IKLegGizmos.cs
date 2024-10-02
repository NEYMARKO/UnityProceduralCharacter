using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKLegGizmos : MonoBehaviour
{
    [Header("Leg IK")]
    [SerializeField]
    Transform leftKnee;
    [SerializeField]
    Transform rightKnee;
    [SerializeField]
    [Range(1, 10)]
    int numberOfRays;
    [SerializeField]
    [Range(1.0f, 4.0f)]
    float rayPlaneDimensions;

    Vector3[] raysOrigins;
    RaycastHit[] sphereCastHits;
    // Start is called before the first frame update
    void Start()
    {
        float rayOffset = rayPlaneDimensions / numberOfRays;
        raysOrigins = new Vector3[numberOfRays * 2];
        sphereCastHits = new RaycastHit[numberOfRays * 2];

        for (int i = 0; i < numberOfRays * 2; i++)
        {
            Vector3 offsetVector = (i / 2) * transform.forward * rayOffset;
            raysOrigins[i] = i % 2 == 0 ? leftKnee.position + offsetVector : rightKnee.position + offsetVector;
        }
    }

    // Update is called once per frame
    void Update()
    {
        CastRays();
        UpdateRaysOrigins();
    }

    private void CastRays()
    {
        for (int i = 0; i < raysOrigins.Length; i++)
        {
            RaycastHit hit;
            Physics.SphereCast(raysOrigins[i], 0.1f, Vector3.down, out hit, 1.5f, LayerMask.GetMask("Terrain"));
            sphereCastHits[i] = hit;
        }
    }

    private void UpdateRaysOrigins()
    {
        float rayOffset = rayPlaneDimensions / numberOfRays;
        for (int i = 0; i < raysOrigins.Length; i++)
        {
            Vector3 offset = transform.forward * (i / 2) * rayOffset;
            raysOrigins[i] = i % 2 == 0 ? leftKnee.position + offset : rightKnee.position + offset;
        }
    }

    private void OnDrawGizmos()
    {
        if (sphereCastHits == null) return;
        Gizmos.color = Color.red;
        for (int i = 0;i < raysOrigins.Length;i++)
        {
            Gizmos.DrawSphere(sphereCastHits[i].point, 0.1f);
            Gizmos.DrawCube(raysOrigins[i], new Vector3(0.1f, 0.1f, 0.1f));
            Gizmos.DrawLine(raysOrigins[i], raysOrigins[i] + Vector3.down * 1.5f);
        }
    }
}
