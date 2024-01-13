using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// A simple job that adds two integers
[BurstCompile]
public struct DrawCatmullSplineJob : IJobParallelFor
{
    private Matrix4x4 Matrix;
    private float drawStep;
    public NativeArray<Vector3> result;
    [ReadOnly]
    public NativeArray<Vector3> knots;
    private float T;
    public DrawCatmullSplineJob(Matrix4x4 matrix, NativeArray<Vector3> knots, NativeArray<Vector3> result, float drawStep, float T)
    {
        this.Matrix = matrix;
        this.result = result;
        this.knots = knots;
        this.drawStep = drawStep;
        this.T = T;
    }

    public void Execute(int index)
    {
        // Calculate the total number of points per segment based on the drawStep.
        int totalPointsPerSegment = Mathf.CeilToInt(1f / drawStep);

        // Calculate which segment this index is in, taking into account T's starting point.
        int segmentIndex = index / totalPointsPerSegment + Mathf.FloorToInt(T);

        // Calculate the local t value for this index within its segment.
        float t = (index % totalPointsPerSegment) * drawStep;

        // Correct t based on the fractional part of T if this is the first segment being drawn.
        if (segmentIndex == Mathf.FloorToInt(T))
        {
            t += T - Mathf.FloorToInt(T);
            // Ensure t is less than 1.0 to avoid going into the next segment.
            t = Mathf.Min(t, 0.999f);
        }

        // Ensure we do not exceed the bounds of the knots array.
        if (segmentIndex < knots.Length - 3)
        {
            // Compute the position for the current t value within the current segment.
            result[index] = CubicSplineAtSegment(t, segmentIndex);
        }
    }

    public Vector3 CubicSplineAtSegment(float t, int segment)
    {
        return ComputePosition(knots[segment], knots[segment + 1], knots[segment + 2], knots[segment + 3], t);
    }

    public Vector3 ComputePosition(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // Transpose the matrix to set p0, p1, p2, p3 as rows
        Matrix4x4 pointsMatrix = new Matrix4x4(p0, p1, p2, p3).transpose;

        Vector4 intermediate = new Vector4(
            MultiplyVectors(tVector(t), Matrix.GetColumn(0)),
            MultiplyVectors(tVector(t), Matrix.GetColumn(1)),
            MultiplyVectors(tVector(t), Matrix.GetColumn(2)),
            MultiplyVectors(tVector(t), Matrix.GetColumn(3))
            );
        Vector4 result = new Vector4(
             MultiplyVectors(intermediate, pointsMatrix.GetColumn(0)),
            MultiplyVectors(intermediate, pointsMatrix.GetColumn(1)),
            MultiplyVectors(intermediate, pointsMatrix.GetColumn(2)),
            MultiplyVectors(intermediate, pointsMatrix.GetColumn(3))
            );
        return new Vector3(result.x, result.y, result.z);
    }

    public float MultiplyVectors(Vector4 v1, Vector4 v2)
    {
        return (v1.x * v2.x) + (v1.y * v2.y) + (v1.z * v2.z) + (v1.w * v2.w);
    }
    public Vector4 tVector(float t)
    {
        return new Vector4(1, t, t * t, t * t * t);
    }
}

public class CatmullRomSpline : Spline
{
    // Start is called before the first frame update
    public static Matrix4x4 Catmull_Rom_Matrix_Scaled = new Matrix4x4(
    new Vector4(0, -1, 2, -1) * 0.5f,
    new Vector4(2, 0, -5, 3) * 0.5f,
    new Vector4(0, 1, 4, -3) * 0.5f,
    new Vector4(0, 0, -1, 1) * 0.5f
    );

    private int lookAheadCoefficient = 3;

    protected override int LookAheadCoefficient()
    {
        return lookAheadCoefficient;
    }

    private void Awake()
    {
        lookAheadCoefficient = 3;
    }

    protected override Vector3 CubicSplineAtSegment(float t, int segment)
    {   
        return Compute(Knots[segment].transform.position, Knots[segment + 1].transform.position,
                    Knots[segment + 2].transform.position, Knots[segment + 3].transform.position, t % Mathf.Max(segment, 1), tVector);
    }

    protected override Matrix4x4 CoefficientMatrix()
    {
        return Catmull_Rom_Matrix_Scaled;
    }

    public override Vector3 DerivativeAtSegment(float t, int segment)
    {
        return Compute(Knots[segment].transform.position, Knots[segment + 1].transform.position, Knots[segment + 2].transform.position,
            Knots[segment + 3].transform.position, t, tVectorDerivative);
    }

    protected override JobHandle DrawSplineMultithreaded()
    {
        int positionsCount = Mathf.FloorToInt(Knots.Count * (1 / drawStep)) - LookAheadCoefficient() * Mathf.FloorToInt(1 / drawStep)
                - Mathf.FloorToInt(T) * 10;
        splinePositions = new NativeArray<Vector3>(positionsCount, Allocator.TempJob);
        for (int i = 0; i < Knots.Count; i++)
        {
            knotsPositions[i] = Knots[i].transform.position;
        }
        //Introduce logic to select spline job depending on specific spline type
        //For now, by default it is a Catmull rom spline
        DrawCatmullSplineJob job = new DrawCatmullSplineJob(Matrix, knotsPositions, splinePositions, 0.1f, T);
        return job.Schedule(positionsCount, 1);
    }
}
