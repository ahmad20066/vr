using System.Collections.Generic;
using UnityEngine;

public class Octree<T>
{
    private class Node
    {
        public Bounds Bounds;
        public List<T> Objects;
        public Node[] Children;
        public bool IsLeaf => Children == null;

        public Node(Bounds bounds)
        {
            Bounds = bounds;
            Objects = new List<T>();
        }

        public void Subdivide()
        {
            Children = new Node[8];
            Vector3 size = Bounds.size / 2f;
            Vector3 center = Bounds.center;
            for (int i = 0; i < 8; i++)
            {
                Vector3 offset = new Vector3(
                    (i & 1) == 0 ? -size.x / 2 : size.x / 2,
                    (i & 2) == 0 ? -size.y / 2 : size.y / 2,
                    (i & 4) == 0 ? -size.z / 2 : size.z / 2
                );
                Bounds childBounds = new Bounds(center + offset, size);
                Children[i] = new Node(childBounds);
            }
        }
    }

    private Node root;
    private int maxDepth;
    private int maxObjectsPerNode;

    public Octree(Bounds bounds, int maxDepth = 8, int maxObjectsPerNode = 4)
    {
        root = new Node(bounds);
        this.maxDepth = maxDepth;
        this.maxObjectsPerNode = maxObjectsPerNode;
    }

    public void Insert(T obj, Bounds objBounds)
    {
        Insert(root, obj, objBounds, 0);
    }

    private void Insert(Node node, T obj, Bounds objBounds, int depth)
    {
        if (!node.Bounds.Intersects(objBounds)) return;

        if (node.IsLeaf)
        {
            node.Objects.Add(obj);

            if (node.Objects.Count > maxObjectsPerNode && depth < maxDepth)
            {
                node.Subdivide();

                foreach (var item in node.Objects)
                {
                    foreach (var child in node.Children)
                    {
                        if (child.Bounds.Intersects(objBounds))
                        {
                            Insert(child, item, objBounds, depth + 1);
                        }
                    }
                }
                node.Objects.Clear();
            }
        }
        else
        {
            foreach (var child in node.Children)
            {
                Insert(child, obj, objBounds, depth + 1);
            }
        }
    }

    public List<T> Query(Bounds queryBounds)
    {
        List<T> results = new List<T>();
        Query(root, queryBounds, results);
        return results;
    }

    private void Query(Node node, Bounds queryBounds, List<T> results)
    {
        if (!node.Bounds.Intersects(queryBounds)) return;

        if (node.IsLeaf)
        {
            results.AddRange(node.Objects);
        }
        else
        {
            foreach (var child in node.Children)
            {
                Query(child, queryBounds, results);
            }
        }
    }

    // Add methods to get node data for the compute shader
    private void GatherNodeData(Node node, List<Vector3> centers, List<Vector3> sizes, List<int> objectCounts, List<int> objects)
    {
        centers.Add(node.Bounds.center);
        sizes.Add(node.Bounds.size);
        objectCounts.Add(node.Objects.Count);
        objects.AddRange((IEnumerable<int>)node.Objects);
        if (!node.IsLeaf)
        {
            foreach (var child in node.Children)
            {
                GatherNodeData(child, centers, sizes, objectCounts, objects);
            }
        }
    }

    public void GetOctreeData(out Vector3[] centers, out Vector3[] sizes, out int[] objectCounts, out int[] objects)
    {
        List<Vector3> nodeCenters = new List<Vector3>();
        List<Vector3> nodeSizes = new List<Vector3>();
        List<int> nodeObjectCounts = new List<int>();
        List<int> nodeObjects = new List<int>();

        GatherNodeData(root, nodeCenters, nodeSizes, nodeObjectCounts, nodeObjects);

        centers = nodeCenters.ToArray();
        sizes = nodeSizes.ToArray();
        objectCounts = nodeObjectCounts.ToArray();
        objects = nodeObjects.ToArray();
    }
}
