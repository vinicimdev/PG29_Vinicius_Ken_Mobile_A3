using System.Collections.Generic;
using UnityEngine;

public class SwipeSlicer : MonoBehaviour
{
    [SerializeField, Tooltip("")]
    private SwipeTracker _swipeTracker;

    [SerializeField, Tooltip("")]
    private Camera _camera;

    [SerializeField, Tooltip("")]
    private float _hitRadiusPixels = 60f;

    private static readonly List<Sliceable> s_active = new List<Sliceable>(32);

    public static void Register(Sliceable sliceable)
    {
        if (s_active.Contains(sliceable) == false)
        {
            s_active.Add(sliceable);
        }
    }

    public static void Unregister(Sliceable sliceable)
    {
        s_active.Remove(sliceable);
    }

    private Vector2 _prevScreen;
    private bool _wasSwiping;

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    private void Update()
    {
        if (_swipeTracker == null || _camera == null)
        {
            return;
        }

        bool isSwiping = _swipeTracker.IsSwiping;

        if (isSwiping == true && _wasSwiping == false)
        {
            _prevScreen = _swipeTracker.SwipeStartScreen;
        }
        _wasSwiping = isSwiping;

        if (isSwiping == false)
        {
            return;
        }

        Vector2 currentScreen = _swipeTracker.CurrentScreen;

        if ((currentScreen - _prevScreen).sqrMagnitude < 0.5f)
        {
            return;
        }

        Vector2 swipeStart = _swipeTracker.SwipeStartScreen;

        for (int i = s_active.Count - 1; i >= 0; i--)
        {
            Sliceable sliceable = s_active[i];

            if (sliceable == null)
            {
                s_active.RemoveAt(i);
                continue;
            }

            Vector3 screenPos3 = _camera.WorldToScreenPoint(sliceable.transform.position);
            if (screenPos3.z <= 0f)
            {
                continue;
            }

            Vector2 screenPos = new Vector2(screenPos3.x, screenPos3.y);
            float dist = DistancePointToSegment(screenPos, _prevScreen, currentScreen);

            if (dist <= _hitRadiusPixels)
            {
                if (SwipePlaneBuilder.TryBuild(_camera, swipeStart, currentScreen, out Plane plane))
                {
                    sliceable.Slice(plane);
                }
            }
        }

        _prevScreen = currentScreen;
    }

    private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float abLenSqr = ab.sqrMagnitude;

        if (abLenSqr < 1e-6f)
        {
            return Vector2.Distance(p, a);
        }

        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / abLenSqr);
        Vector2 closest = a + t * ab;
        return Vector2.Distance(p, closest);
    }
}