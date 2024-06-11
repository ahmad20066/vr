using System.Collections.Generic;
using UnityEngine;

public class KDTree
{
    private class KDNode
    {
        public int index;
        public Vector3 point;
        public KDNode left;
        public KDNode right;
        public int depth;
    }

    private KDNode root;
    private List<Vector3> points;

    public KDTree(Vector3[] points)
    {
        this.points = new List<Vector3>(points);
        root = BuildTree(0, points.Length, 0);
    }

    private KDNode BuildTree(int start, int end, int depth)
    {
        if (end <= start) return null;

        int axis = depth % 3;
        int mid = (start + end) / 2;

        if (axis == 0)
            this.points.Sort(start, end - start, new XComparer());
        else if (axis == 1)
            this.points.Sort(start, end - start, new YComparer());
        else
            this.points.Sort(start, end - start, new ZComparer());

        KDNode node = new KDNode
        {
            index = mid,
            point = this.points[mid],
            depth = depth,
            left = BuildTree(start, mid, depth + 1),
            right = BuildTree(mid + 1, end, depth + 1)
        };

        return node;
    }

    public List<int> RangeQuery(Vector3 center, float radius)
    {
        List<int> result = new List<int>();
        RangeQuery(root, center, radius, result);
        return result;
    }

    private void RangeQuery(KDNode node, Vector3 center, float radius, List<int> result)
    {
        if (node == null) return;

        float dist = Vector3.Distance(node.point, center);
        if (dist <= radius)
        {
            result.Add(node.index);
        }

        int axis = node.depth % 3;
        float diff = center[axis] - node.point[axis];
        if (diff <= 0)
        {
            RangeQuery(node.left, center, radius, result);
            if (diff * diff <= radius * radius)
            {
                RangeQuery(node.right, center, radius, result);
            }
        }
        else
        {
            RangeQuery(node.right, center, radius, result);
            if (diff * diff <= radius * radius)
            {
                RangeQuery(node.left, center, radius, result);
            }
        }
    }

    private class XComparer : IComparer<Vector3>
    {
        public int Compare(Vector3 a, Vector3 b) => a.x.CompareTo(b.x);
    }

    private class YComparer : IComparer<Vector3>
    {
        public int Compare(Vector3 a, Vector3 b) => a.y.CompareTo(b.y);
    }

    private class ZComparer : IComparer<Vector3>
    {
        public int Compare(Vector3 a, Vector3 b) => a.z.CompareTo(b.z);
    }
}
