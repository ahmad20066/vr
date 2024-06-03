using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KdTree : MonoBehaviour
{
    private KdNode root;

    public void Build(List<Vector3> points)
    {
        root = Build(points, 0);
    }

    private KdNode Build(List<Vector3> points, int depth)
    {
        if (points.Count == 0)
            return null;

        int axis = depth % 3;
        points.Sort((a, b) => a[axis].CompareTo(b[axis]));

        int medianIndex = points.Count / 2;
        KdNode node = new KdNode
        {
            Point = points[medianIndex],
            Left = Build(points.GetRange(0, medianIndex), depth + 1),
            Right = Build(points.GetRange(medianIndex + 1, points.Count - (medianIndex + 1)), depth + 1)
        };
        return node;
    }

    public List<Vector3> RangeSearch(Vector3 point, float radius)
    {
        List<Vector3> results = new List<Vector3>();
        RangeSearch(root, point, radius, 0, results);
        return results;
    }

    private void RangeSearch(KdNode node, Vector3 point, float radius, int depth, List<Vector3> results)
    {
        if (node == null)
            return;

        int axis = depth % 3;
        float distance = Vector3.Distance(node.Point, point);

        if (distance <= radius)
        {
            results.Add(node.Point);
        }

        float delta = point[axis] - node.Point[axis];

        if (delta < 0)
        {
            RangeSearch(node.Left, point, radius, depth + 1, results);
            if (Mathf.Abs(delta) <= radius)
            {
                RangeSearch(node.Right, point, radius, depth + 1, results);
            }
        }
        else
        {
            RangeSearch(node.Right, point, radius, depth + 1, results);
            if (Mathf.Abs(delta) <= radius)
            {
                RangeSearch(node.Left, point, radius, depth + 1, results);
            }
        }
    }
}

public class KdNode
{
    public Vector3 Point;
    public KdNode Left;
    public KdNode Right;
}