using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCombineOnAwake : MonoBehaviour
{
    [SerializeField, Tooltip("")]
    private bool _disableChildRenderers = true;

    private void Awake()
    {
        List<MeshFilter> children = new List<MeshFilter>();
        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>())
        {
            if (mf.gameObject == gameObject)
            {
                continue;
            }
            if (mf.sharedMesh == null)
            {
                continue;
            }

            children.Add(mf);
        }

        if (children.Count == 0)
        {
            Debug.LogWarning($"[{nameof(MeshCombineOnAwake)}] No child meshes to combine on '{name}'.", this);
            return;
        }

        CombineInstance[] combine = new CombineInstance[children.Count];
        int totalVerts = 0;
        for (int i = 0; i < children.Count; i++)
        {
            combine[i].mesh = children[i].sharedMesh;
            combine[i].transform = transform.worldToLocalMatrix * children[i].transform.localToWorldMatrix;
            totalVerts += children[i].sharedMesh.vertexCount;
        }

        Mesh combined = new Mesh();
        combined.name = name + "_Combined";

        // 16-bit index buffer caps at 65535 vertices;
        // switch to 32-bit if needed.
        if (totalVerts > 65535)
        {
            combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        combined.CombineMeshes(combine, mergeSubMeshes: false);
        combined.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = combined;

        Material[] materials = new Material[children.Count];
        for (int i = 0; i < children.Count; i++)
        {
            MeshRenderer childMR = children[i].GetComponent<MeshRenderer>();
            materials[i] = childMR != null && childMR.sharedMaterials.Length > 0
                ? childMR.sharedMaterials[0]
                : null;
        }
        GetComponent<MeshRenderer>().sharedMaterials = materials;

        if (_disableChildRenderers == true)
        {
            for (int i = 0; i < children.Count; i++)
            {
                MeshRenderer childMR = children[i].GetComponent<MeshRenderer>();
                if (childMR != null) childMR.enabled = false;
            }
        }
    }
}