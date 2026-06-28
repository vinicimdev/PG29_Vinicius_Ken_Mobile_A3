using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script is used to gather vertex, normals, UVs and triangles data to build a Mesh.
/// This is used by the MeshCutter to create each side of the split (cut) object.
/// </summary>
public class MeshBuilder
{
    // Store vertices, normals and UVs to build the triangles to build the mesh
    private readonly List<Vector3> _vertices = new List<Vector3>(1024);
    private readonly List<Vector3> _normals = new List<Vector3>(1024);
    private readonly List<Vector2> _uvs = new List<Vector2>(1024);

    // One triangle index list per submesh
    private readonly List<List<int>> _submeshTriangles = new List<List<int>>(4);

    // Separate triangle list for the cap
    private readonly List<int> _capTriangles = new List<int>(256);

    private int _submeshCount;

    public int VertexCount => _vertices.Count;

    public void Reset(int submeshCount)
    {
        _submeshCount = submeshCount;

        _vertices.Clear();
        _normals.Clear();
        _uvs.Clear();
        _capTriangles.Clear();

        while (_submeshTriangles.Count < submeshCount)
        {
            _submeshTriangles.Add(new List<int>(512));
        }

        for (int i = 0; i < submeshCount; i++)
        {
            _submeshTriangles[i].Clear();
        }
    }

    /// <summary>
    /// Adds a triangle to the mesh. Each triangle needs 3 vertices, 3 normals and 3 UVs.
    /// </summary>
    public void AddTriangle(int submeshIndex,
                            Vector3 v0, Vector3 v1, Vector3 v2,
                            Vector3 n0, Vector3 n1, Vector3 n2,
                            Vector3 u0, Vector3 u1, Vector3 u2)
    {
        // The base index is where the 3 new vertices started being inserted
        // If we already have 3 vertices, base index = 3
        // so the new triangle will use vertices at indices 3, 4 and 5
        int baseIndex = _vertices.Count;

        // Add the passed vertices, normals and UVs to their respective lists
        _vertices.Add(v0);
        _vertices.Add(v1);
        _vertices.Add(v2);

        _normals.Add(n0);
        _normals.Add(n1);
        _normals.Add(n2);

        _uvs.Add(u0);
        _uvs.Add(u1);
        _uvs.Add(u2);

        List<int> triList = _submeshTriangles[submeshIndex];
        triList.Add(baseIndex);
        triList.Add(baseIndex + 1);
        triList.Add(baseIndex + 2);
    }

    // Same as the AddTriangle
    /// <summary>
    /// Stores the triangle indices in the cap submesh, so we can use different materials for the cap and the original mesh.
    /// </summary>
    public void AddCapTriangle(Vector3 v0, Vector3 v1, Vector3 v2,
                               Vector3 n0, Vector3 n1, Vector3 n2,
                               Vector3 u0, Vector3 u1, Vector3 u2)
    {
        int baseIndex = _vertices.Count;

        _vertices.Add(v0);
        _vertices.Add(v1);
        _vertices.Add(v2);

        _normals.Add(n0);
        _normals.Add(n1);
        _normals.Add(n2);

        _uvs.Add(u0);
        _uvs.Add(u1);
        _uvs.Add(u2);

        _capTriangles.Add(baseIndex);
        _capTriangles.Add(baseIndex + 1);
        _capTriangles.Add(baseIndex + 2);

    }

    public Mesh Build()
    {
        // Create a new empty mesh and set a name for debugging
        var mesh = new Mesh();
        mesh.name = "CutMesh";

        // Unity allocates 16-bit indices for triangles by default,
        // which stores up to 65535 vertices
        // If the mesh has more than that, we change the index format to 32-bit
        // so we can store more vertices.
        // We only do this when the mesh needs it, otherwise, for smaller meshes we let unity
        // use the 16-bit index.
        if (_vertices.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        // Set the mesh data using the lists we built
        mesh.SetVertices(_vertices);
        mesh.SetNormals(_normals);
        mesh.SetUVs(0, _uvs);

        mesh.subMeshCount = _submeshCount + 1;

        for (int sm = 0; sm < _submeshCount; sm++)
        {
            mesh.SetTriangles(_submeshTriangles[sm], sm);
        }

        mesh.SetTriangles(_capTriangles, _submeshCount);

        // Recalculate the bounds of the mesh so Unity can render it properly
        // and do other things under the hood.
        mesh.RecalculateBounds();

        return mesh;
    }
}
