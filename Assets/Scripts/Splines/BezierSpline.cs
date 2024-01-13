using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public class BezierSpline : Spline
{
    // Start is called before the first frame update
    public static Matrix4x4 BezierMatrix = new Matrix4x4(
    new Vector4(1, -3, 3, -1),
    new Vector4(0, 3, -6, 3),
    new Vector4(0, 0, 3, -3),
    new Vector4(0, 0, 0, 1)
    );

    private int lookAheadCoefficient = 1;

    protected override int LookAheadCoefficient()
    {
        return lookAheadCoefficient;
    }

    protected override Vector3 CubicSplineAtSegment(float t, int segment)
    {
        return Compute(Knots[segment].transform.position,
            ((BezierSplineKnot)Knots[segment]).mainHandle.transform.position,
            ((BezierSplineKnot)Knots[segment + 1]).constrainedHandle.transform.position,
                    Knots[segment + 1].transform.position,
                    t % Mathf.Max(segment, 1), tVector);
    }

    protected override Matrix4x4 CoefficientMatrix()
    {
        return BezierMatrix;
    }

    // Note that the derivative doesn't represent the real velocity as Bezier curves are not C1 continuous (see Catmull splines)
    // Here it is rather the direction and rate of
    // change of the curve at a given point (so influenced by the segments handles), which is essential for smooth animations, path following,
    // and other applications where the shape and behavior of the curve are important.
    // We can derive a velocity by constraining the handles so that they are mirrored
    // Not C2 continuous
    public override Vector3 DerivativeAtSegment(float t, int segment)
    {
        return Compute(Knots[segment].transform.position,
            ((BezierSplineKnot)Knots[segment]).mainHandle.transform.position,
            ((BezierSplineKnot)Knots[segment + 1]).constrainedHandle.transform.position,
                    Knots[segment + 1].transform.position,
                    t % Mathf.Max(segment, 1), tVectorDerivative).normalized;
    }

    protected override JobHandle DrawSplineMultithreaded()
    {
        throw new System.NotImplementedException();
    }
}
