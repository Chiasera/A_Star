using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BezierSplineKnot : SplineKnot
{
    public Transform mainHandle;
    public Transform constrainedHandle;
    Vector3 handleDirection;
    
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (Selection.Contains(this.gameObject) || Selection.Contains(mainHandle.gameObject) || Selection.Contains(constrainedHandle.gameObject))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(mainHandle.transform.position, 0.1f);
            Gizmos.DrawSphere(constrainedHandle.transform.position, 0.1f);
            Gizmos.DrawLine(mainHandle.position, constrainedHandle.position);
        }
    }

    private void Update()
    {
        handleDirection = mainHandle.transform.position - transform.position;
        constrainedHandle.transform.position = transform.position - handleDirection;
    }
}
