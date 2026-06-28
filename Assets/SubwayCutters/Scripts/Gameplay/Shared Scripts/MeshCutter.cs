
using System.Collections.Generic;
using UnityEngine;

public static class MeshCutter
{
    // Small tolerance to avoid floating point bugs
    // vertices exactly on the plane could cause problems
    // so we consider "really close to 0" as positive side
    private const float EPSILON = 0.00001f;

    // Static scratch buffers
    // Cut() is called on the main thread and never re-entered, so reusing these is safe
    // Cleans GC allocs
    private static readonly List<Vector3> s_vertexBuffer = new List<Vector3>(1024);
    private static readonly List<Vector3> s_normalBuffer = new List<Vector3>(1024);
    private static readonly List<Vector2> s_uvBuffer = new List<Vector2>(1024);
    private static readonly List<int> s_triangleBuffer = new List<int>(2048);
    private static float[] s_sidesBuffer = new float[1024];

    private static readonly MeshBuilder s_builderA = new MeshBuilder();
    private static readonly MeshBuilder s_builderB = new MeshBuilder();

    private static readonly List<(Vector3 a, Vector3 b)> s_cutEdges = new List<(Vector3, Vector3)>(256);

    private static readonly Vector3[] s_splitVerts = new Vector3[3];
    private static readonly Vector3[] s_splitNormals = new Vector3[3];
    private static readonly Vector2[] s_splitUVs = new Vector2[3];
    private static readonly float[] s_splitSides = new float[3];

    private static readonly List<(Vector3 v, Vector3 n, Vector2 u)> s_positivePoly = new List<(Vector3, Vector3, Vector2)>(4);
    private static readonly List<(Vector3 v, Vector3 n, Vector2 u)> s_negativePoly = new List<(Vector3, Vector3, Vector2)>(4);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_vertexBuffer.Clear();
        s_normalBuffer.Clear();
        s_uvBuffer.Clear();
        s_triangleBuffer.Clear();
        s_sidesBuffer = new float[1024];

        s_cutEdges.Clear();
        s_positivePoly.Clear();
        s_negativePoly.Clear();

        s_builderA.Reset(0);
        s_builderB.Reset(0);
    }

    public static bool Cut(Mesh original, Transform meshTransform, Plane worldPlane,
                           out Mesh meshA, out Mesh meshB)
    {
        meshA = null;
        meshB = null;

        // Convert plane to local space of the mesh
        Plane localPlane = TransformPlaneToLocal(worldPlane, meshTransform);

        original.GetVertices(s_vertexBuffer);
        original.GetNormals(s_normalBuffer);
        original.GetUVs(0, s_uvBuffer);
        int submeshCount = original.subMeshCount;
        int vertexCount = s_vertexBuffer.Count;

        // Grow the sides array only when a bigger mesh comes through
        // Power of two to avoid thrashing
        if (s_sidesBuffer.Length < vertexCount)
        {
            s_sidesBuffer = new float[Mathf.NextPowerOfTwo(vertexCount)];
        }

        // Classify every vertex by comparing its distance to the arbitrary plane
        // +1 = positive side, -1 = negative side, 0 = exactly on the plane`
        for (int i = 0; i < vertexCount; i++)
        {
            s_sidesBuffer[i] = localPlane.GetDistanceToPoint(s_vertexBuffer[i]);
        }

        s_builderA.Reset(submeshCount);
        s_builderB.Reset(submeshCount);

        s_cutEdges.Clear();

        for (int sm = 0; sm < submeshCount; sm++)
        {
            original.GetTriangles(s_triangleBuffer, sm);
            int triIndexCount = s_triangleBuffer.Count;

            for (int i = 0; i < triIndexCount; i += 3)
            {
                // Indexes of the vertices of the triangle
                int i0 = s_triangleBuffer[i];
                int i1 = s_triangleBuffer[i + 1];
                int i2 = s_triangleBuffer[i + 2];

                // Distance from the plane to the first vertex
                float s0 = s_sidesBuffer[i0];
                float s1 = s_sidesBuffer[i1];
                float s2 = s_sidesBuffer[i2];

                // Checks if the vertices are on the positive or on the negative side,
                // based on the epsilon
                bool a0 = s0 >= -EPSILON;
                bool a1 = s1 >= -EPSILON;
                bool a2 = s2 >= -EPSILON;

                // All triangles are on the same side, so no cut needed
                if (a0 && a1 && a2)
                {
                    s_builderA.AddTriangle(sm,
                                         s_vertexBuffer[i0], s_vertexBuffer[i1], s_vertexBuffer[i2],
                                         s_normalBuffer[i0], s_normalBuffer[i1], s_normalBuffer[i2],
                                         GetUV(i0), GetUV(i1), GetUV(i2));
                    continue;
                }
                if (a0 == false && a1 == false && a2 == false)
                {
                    s_builderB.AddTriangle(sm,
                                         s_vertexBuffer[i0], s_vertexBuffer[i1], s_vertexBuffer[i2],
                                         s_normalBuffer[i0], s_normalBuffer[i1], s_normalBuffer[i2],
                                         GetUV(i0), GetUV(i1), GetUV(i2));
                    continue;
                }

                // Triangle intersecting the plane, split it
                SplitTriangle(sm,
                              s_vertexBuffer[i0], s_vertexBuffer[i1], s_vertexBuffer[i2],
                              s_normalBuffer[i0], s_normalBuffer[i1], s_normalBuffer[i2],
                              GetUV(i0), GetUV(i1), GetUV(i2),
                              s0, s1, s2,
                              s_builderA, s_builderB,
                              s_cutEdges);
            }

        }

        // The mesh needs geometry on both sides, so if there is no geo, don'intersectionRatio cut
        // or if one of the sides is empty, the cut didn'intersectionRatio actually cut the mesh,
        // so we return false (no cut happened)
        if (s_builderA.VertexCount == 0 || s_builderB.VertexCount == 0)
        {
            return false;
        }

        // Generate the cap faces on the cut plane if there are at least
        // 3 cut points (minimum to form a polygon)
        if (s_cutEdges.Count >= 3)
        {
            GenerateCap(s_cutEdges, localPlane.normal, s_builderA, s_builderB);
        }

        // Builds the meshes
        meshA = s_builderA.Build();
        meshB = s_builderB.Build();

        return true;
    }

    // This method is used when a triangle crosses the cutting plane.
    // Some vertices are on one side, others on the other side, so the 
    // method splits this triangle in smaller pieces that are totally 
    // on one side.
    private static void SplitTriangle(int submeshIndex,
                                      Vector3 v0, Vector3 v1, Vector3 v2,
                                      Vector3 n0, Vector3 n1, Vector3 n2,
                                      Vector2 u0, Vector2 u1, Vector2 u2,
                                      float s0, float s1, float s2,
                                      MeshBuilder positiveBuilder, MeshBuilder negativeBuilder,
                                      List<(Vector3 a, Vector3 b)> cutEdges)
    {
        s_splitVerts[0] = v0;
        s_splitVerts[1] = v1;
        s_splitVerts[2] = v2;

        s_splitNormals[0] = n0;
        s_splitNormals[1] = n1;
        s_splitNormals[2] = n2;

        s_splitUVs[0] = u0;
        s_splitUVs[1] = u1;
        s_splitUVs[2] = u2;

        s_splitSides[0] = s0;
        s_splitSides[1] = s1;
        s_splitSides[2] = s2;

        s_positivePoly.Clear();
        s_negativePoly.Clear();

        Vector3 firstIntersection = Vector3.zero;
        Vector3 secondIntersection = Vector3.zero;
        int intersectionCount = 0;

        for (int current = 0; current < 3; current++)
        {
            int next = (current + 1) % 3;

            bool isCurrentSidePositive = s_splitSides[current] >= -EPSILON;
            bool isNextSidePositive = s_splitSides[next] >= -EPSILON;

            if (isCurrentSidePositive == true)
            {
                s_positivePoly.Add((s_splitVerts[current], s_splitNormals[current], s_splitUVs[current]));
            }
            else
            {
                s_negativePoly.Add((s_splitVerts[current], s_splitNormals[current], s_splitUVs[current]));
            }

            if (isCurrentSidePositive != isNextSidePositive)
            {
                float intersectionRatio = s_splitSides[current] / (s_splitSides[current] - s_splitSides[next]);
                Vector3 intersectionPosition = Vector3.Lerp(s_splitVerts[current], s_splitVerts[next], intersectionRatio);
                Vector3 intersectionNormal = Vector3.Lerp(s_splitNormals[current], s_splitNormals[next], intersectionRatio).normalized;
                Vector2 intersectionUV = Vector2.Lerp(s_splitUVs[current], s_splitUVs[next], intersectionRatio);

                s_positivePoly.Add((intersectionPosition, intersectionNormal, intersectionUV));
                s_negativePoly.Add((intersectionPosition, intersectionNormal, intersectionUV));

                // Store the 2 intersection points of this triangle, they form one edge of the cap boundary
                if (intersectionCount == 0)
                {
                    firstIntersection = intersectionPosition;
                }
                else
                {
                    secondIntersection = intersectionPosition;
                }
                intersectionCount++;
            }
        }

        // Each cut triangle produces exactly 2 intersections forming one edge of the cap's boundary
        if (intersectionCount == 2)
        {
            cutEdges.Add((firstIntersection, secondIntersection));
        }

        FanTriangulate(submeshIndex, s_positivePoly, positiveBuilder);
        FanTriangulate(submeshIndex, s_negativePoly, negativeBuilder);
    }

    // Fan triangulate a convex polygon (3 or 4 vertices after splitting)
    // Note: "fan triangulation is a simple way to triangulate a polygon by choosing a vertex and drawing
    // edges to all of the other vertices of the polygon." - Wikipedia :D
    private static void FanTriangulate(int submeshIndex,
                                       List<(Vector3 v, Vector3 n, Vector2 u)> poly, MeshBuilder builder)
    {
        // If the polygon has less than 3 vertices, return
        // we can't triangulate something with less than that
        if (poly.Count < 3)
        {
            return;
        }

        for (int i = 1; i < poly.Count - 1; i++)
        {
            builder.AddTriangle(submeshIndex,
                                poly[0].v, poly[i].v, poly[i + 1].v,
                                poly[0].n, poly[i].n, poly[i + 1].n,
                                poly[0].u, poly[i].u, poly[i + 1].u);
        }
    }

    private static void GenerateCap(List<(Vector3 a, Vector3 b)> cutEdges, Vector3 planeNormal,
                                 MeshBuilder positiveBuilder, MeshBuilder negativeBuilder)
    {
        // Build closed loops from the edges. Each loop is a separate "island" in the cut plane
        // (a hollow object can have multiple loops: outer shell + inner shell, for example).
        List<List<Vector3>> loops = BuildLoops(cutEdges, EPSILON);

        if (loops.Count == 0)
        {
            return;
        }

        Vector3 capNormal = planeNormal.normalized;

        // Compute tangent/bitangent once, used for UVs
        Vector3 tangent = Vector3.Cross(capNormal, Vector3.up);
        if (tangent.sqrMagnitude < 0.01f)
        {
            tangent = Vector3.Cross(capNormal, Vector3.right);
        }
        tangent = tangent.normalized;
        Vector3 bitangent = Vector3.Cross(capNormal, tangent).normalized;

        foreach (List<Vector3> loop in loops)
        {
            if (loop.Count < 3)
            {
                continue;
            }

            Vector3 center = ComputeCentroid(loop);
            EarClipTriangulate(loop, capNormal, tangent, bitangent, center, positiveBuilder, negativeBuilder);
        }
    }

    // Builds closed loops from a list of edges using graph traversal.
    // Each loop represents an "island" of the cut boundary, separate holes/shells get separate loops.
    private static List<List<Vector3>> BuildLoops(List<(Vector3 a, Vector3 b)> edges, float threshold)
    {
        List<List<Vector3>> loops = new List<List<Vector3>>();
        bool[] used = new bool[edges.Count];

        for (int startIndex = 0; startIndex < edges.Count; startIndex++)
        {
            if (used[startIndex] == true)
            {
                continue;
            }

            List<Vector3> currentLoop = new List<Vector3>();
            currentLoop.Add(edges[startIndex].a);
            Vector3 lookFor = edges[startIndex].b;
            used[startIndex] = true;

            // Follow the chain of edges until we come back to the start or run out
            bool foundNext = true;
            while (foundNext == true)
            {
                foundNext = false;
                for (int i = 0; i < edges.Count; i++)
                {
                    if (used[i] == true)
                    {
                        continue;
                    }

                    if ((edges[i].a - lookFor).sqrMagnitude < threshold * threshold)
                    {
                        currentLoop.Add(edges[i].a);
                        lookFor = edges[i].b;
                        used[i] = true;
                        foundNext = true;
                        break;
                    }

                    if ((edges[i].b - lookFor).sqrMagnitude < threshold * threshold)
                    {
                        currentLoop.Add(edges[i].b);
                        lookFor = edges[i].a;
                        used[i] = true;
                        foundNext = true;
                        break;
                    }
                }
            }

            // Only keep loops with enough points to form a polygon
            if (currentLoop.Count >= 3)
            {
                loops.Add(currentLoop);
            }
        }

        return loops;
    }

    private static void EarClipTriangulate(List<Vector3> loop, Vector3 capNormal,
                                        Vector3 tangent, Vector3 bitangent,
                                        Vector3 center,
                                        MeshBuilder positiveBuilder, MeshBuilder negativeBuilder)
    {
        // Project 3D loop points to 2D using the plane's tangent/bitangent basis
        List<Vector2> points2D = new List<Vector2>(loop.Count);
        for (int i = 0; i < loop.Count; i++)
        {
            Vector3 d = loop[i] - center;
            points2D.Add(new Vector2(Vector3.Dot(d, tangent), Vector3.Dot(d, bitangent)));
        }

        // Determine winding order via signed area. Ear clipping needs CCW (counter clockwise) polygons.
        float signedArea = 0f;
        for (int i = 0; i < points2D.Count; i++)
        {
            Vector2 a = points2D[i];
            Vector2 b = points2D[(i + 1) % points2D.Count];
            signedArea += (b.x - a.x) * (b.y + a.y);
        }
        if (signedArea > 0f)
        {
            loop.Reverse();
            points2D.Reverse();
        }

        // Active vertex indices - start with all, remove as we find ears
        List<int> indices = new List<int>(points2D.Count);
        for (int i = 0; i < points2D.Count; i++)
        {
            indices.Add(i);
        }

        // Safety cap to prevent infinite loop on malformed input
        int safetyLimit = points2D.Count * points2D.Count;
        int iterations = 0;

        while (indices.Count > 3 && iterations < safetyLimit)
        {
            iterations++;
            bool earFound = false;

            for (int j = 0; j < indices.Count; j++)
            {
                int prevIdx = indices[(j - 1 + indices.Count) % indices.Count];
                int currIdx = indices[j];
                int nextIdx = indices[(j + 1) % indices.Count];

                Vector2 prev = points2D[prevIdx];
                Vector2 curr = points2D[currIdx];
                Vector2 next = points2D[nextIdx];

                // Cross product < 0 means this vertex is convex
                float cross = (next.x - prev.x) * (curr.y - prev.y) - (next.y - prev.y) * (curr.x - prev.x);
                if (cross >= 0f)
                {
                    continue;
                }

                // No other vertex of the polygon can be inside this triangle for it to be a valid ear
                bool anyInside = false;
                for (int k = 0; k < indices.Count; k++)
                {
                    if (k == j || k == (j - 1 + indices.Count) % indices.Count || k == (j + 1) % indices.Count)
                    {
                        continue;
                    }

                    if (IsPointInTriangle(points2D[indices[k]], prev, curr, next) == true)
                    {
                        anyInside = true;
                        break;
                    }
                }

                if (anyInside == true)
                {
                    continue;
                }

                // Valid ear found, emit triangle and remove the vertex
                EmitCapTriangle(positiveBuilder, negativeBuilder,
                                loop[prevIdx], loop[currIdx], loop[nextIdx],
                                points2D[prevIdx], points2D[currIdx], points2D[nextIdx],
                                capNormal);
                indices.RemoveAt(j);
                earFound = true;
                break;
            }

            if (earFound == false)
            {
                break;
            }
        }

        // The last 3 vertices form the final triangle
        if (indices.Count == 3)
        {
            EmitCapTriangle(positiveBuilder, negativeBuilder,
                            loop[indices[0]], loop[indices[1]], loop[indices[2]],
                            points2D[indices[0]], points2D[indices[1]], points2D[indices[2]],
                            capNormal);
        }
    }

    private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);

        bool hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
        bool hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);

        return (hasNeg && hasPos) == false;
    }

    private static void EmitCapTriangle(MeshBuilder positiveBuilder, MeshBuilder negativeBuilder,
                                     Vector3 a3D, Vector3 b3D, Vector3 c3D,
                                     Vector2 a2D, Vector2 b2D, Vector2 c2D,
                                     Vector3 capNormal)
    {
        Vector2 uva = new Vector2(0.5f + a2D.x * 0.5f, 0.5f + a2D.y * 0.5f);
        Vector2 uvb = new Vector2(0.5f + b2D.x * 0.5f, 0.5f + b2D.y * 0.5f);
        Vector2 uvc = new Vector2(0.5f + c2D.x * 0.5f, 0.5f + c2D.y * 0.5f);

        // Positive side: reverse winding so the normal faces away
        positiveBuilder.AddCapTriangle(c3D, b3D, a3D, capNormal, capNormal, capNormal, uvc, uvb, uva);
        // Negative side: original winding with inverted normal
        negativeBuilder.AddCapTriangle(a3D, b3D, c3D, -capNormal, -capNormal, -capNormal, uva, uvb, uvc);
    }

    // This method converts the cutting plane from world space to local space
    private static Plane TransformPlaneToLocal(Plane worldPlane, Transform t)
    {
        // Matrix that converts world coords to local coords of the object
        Matrix4x4 worldToLocal = t.worldToLocalMatrix;
        // Applies rotation and scale differences, used for direction (normals and vectors)
        Vector3 localNormal = worldToLocal.MultiplyVector(worldPlane.normal).normalized;
        // Applies rotation, scale and translation, used for positions
        // worldPlane.normal * (-worldPlane.distance) is how we get a vertex that is in the plane.
        // Unity's Plane struct only stores the normal and distance to origin. 
        // To get a concrete vertex on the cutting plane, we multiply the normal by the negative of the distance. 
        // That vertex is the projection of the origin on the plane!
        Vector3 localPoint = worldToLocal.MultiplyPoint3x4(worldPlane.normal * (-worldPlane.distance));

        return new Plane(localNormal, localPoint);
    }

    // Safe wrapper for accessing the UVs scratch buffer
    private static Vector2 GetUV(int index) => (index < s_uvBuffer.Count) ? s_uvBuffer[index] : Vector2.zero;

    // Centroid is the center of mass of a group of points.
    // It calculates by adding all points and dividing by the amount of points, simple average in 3D
    // Used in the GenerateCap method as a central point for the fan triangulation
    private static Vector3 ComputeCentroid(List<Vector3> points)
    {
        Vector3 sum = Vector3.zero;
        foreach (var p in points)
        {
            sum += p;
        }

        return sum / points.Count;
    }
}

