using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Sliceable : MonoBehaviour
{
    [Header("Cut Physics")]
    [SerializeField, Tooltip("")]
    private float _explosionForce = 3f;
    [SerializeField, Tooltip("")]
    private float _explosionRadius = 1f;
    [SerializeField, Tooltip("")]
    private float _explosionUpward = 0.3f;
    [SerializeField, Tooltip("")]
    private float _spinTorque = 2f;

    [Header("Pieces")]
    [SerializeField, Tooltip("")]
    private float _pieceMass = 0.5f;
    [SerializeField, Tooltip("")]
    private float _pieceLifetime = 3f;

    [Header("Material")]
    [SerializeField, Tooltip("")]
    private Material _capMaterial;

    [HideInInspector] public bool isCutPiece = false;

    public event Action<Sliceable, Plane> Sliced;

    private int _lastSlicedFrame = -1;
    private bool _registered;

    private void Start()
    {
        if (isCutPiece == false)
        {
            SwipeSlicer.Register(this);
            _registered = true;
        }
    }

    private void OnDestroy()
    {
        if (_registered)
        {
            SwipeSlicer.Unregister(this);
        }

        if (isCutPiece)
        {
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Destroy(mf.sharedMesh);
            }
        }
    }

    /// <summary>
    /// Slices this object along the given world-space plane. Spawns two pieces and destroys the original.
    /// Returns true if the cut succeeded (plane intersected the mesh), false otherwise.
    /// </summary>
    public bool Slice(Plane worldPlane)
    {
        if (_lastSlicedFrame == Time.frameCount)
        {
            return false;
        }
        _lastSlicedFrame = Time.frameCount;

        var meshFilter = GetComponent<MeshFilter>();
        var meshRenderer = GetComponent<MeshRenderer>();

        Mesh sourceMesh = meshFilter.sharedMesh;
        if (sourceMesh == null)
        {
            Debug.LogWarning($"[{nameof(Sliceable)}] '{name}' has no mesh assigned, skipping cut.", this);
            return false;
        }

        bool success = MeshCutter.Cut(sourceMesh, transform, worldPlane,
                                      out Mesh meshA, out Mesh meshB);

        if (success == false)
        {
            return false;
        }

        Vector3 explosionCenter = worldPlane.ClosestPointOnPlane(transform.position);

        SpawnPiece(meshA, " [+]", meshRenderer.sharedMaterials, explosionCenter);
        SpawnPiece(meshB, " [-]", meshRenderer.sharedMaterials, explosionCenter);

        Sliced?.Invoke(this, worldPlane);

        Destroy(gameObject);
        return true;
    }

    private void SpawnPiece(Mesh mesh, string suffix, Material[] sourceMaterials, Vector3 explosionCenter)
    {
        var pieceGO = new GameObject(gameObject.name + suffix);
        pieceGO.layer = gameObject.layer;
        pieceGO.transform.SetPositionAndRotation(transform.position, transform.rotation);
        pieceGO.transform.localScale = transform.localScale;

        var pieceMf = pieceGO.AddComponent<MeshFilter>();
        pieceMf.sharedMesh = mesh;

        var pieceMr = pieceGO.AddComponent<MeshRenderer>();
        Material[] combined = new Material[sourceMaterials.Length + 1];
        Array.Copy(sourceMaterials, combined, sourceMaterials.Length);
        combined[sourceMaterials.Length] = _capMaterial != null ? _capMaterial : sourceMaterials[0];
        pieceMr.sharedMaterials = combined;

        var pieceCollider = pieceGO.AddComponent<MeshCollider>();
        pieceCollider.sharedMesh = mesh;
        pieceCollider.convex = true;

        var pieceRb = pieceGO.AddComponent<Rigidbody>();
        pieceRb.mass = _pieceMass;
        pieceRb.AddExplosionForce(_explosionForce, explosionCenter, _explosionRadius, _explosionUpward);
        pieceRb.AddTorque(UnityEngine.Random.insideUnitSphere * _spinTorque, ForceMode.Impulse);

        var pieceSliceable = pieceGO.AddComponent<Sliceable>();
        pieceSliceable.isCutPiece = true;
        pieceSliceable._explosionForce = _explosionForce;
        pieceSliceable._explosionRadius = _explosionRadius;
        pieceSliceable._explosionUpward = _explosionUpward;
        pieceSliceable._spinTorque = _spinTorque;
        pieceSliceable._pieceMass = _pieceMass;
        pieceSliceable._pieceLifetime = _pieceLifetime;
        pieceSliceable._capMaterial = _capMaterial;

        if (_pieceLifetime > 0f)
        {
            Destroy(pieceGO, _pieceLifetime);
        }
    }
}
