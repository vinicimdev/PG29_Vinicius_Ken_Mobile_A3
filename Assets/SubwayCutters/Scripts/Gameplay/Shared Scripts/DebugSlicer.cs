using UnityEngine;

public class DebugSlicer : MonoBehaviour
{
    [SerializeField, Tooltip("The Sliceable that will be cut on each valid swipe.")]
    private Sliceable _target;

    [SerializeField, Tooltip("Swipe input source.")]
    private SwipeTracker _swipeTracker;

    [SerializeField, Tooltip("Camera used to project the swipe into world space.")]
    private Camera _camera;

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    private void OnEnable()
    {
        if (_swipeTracker != null)
        {
            _swipeTracker.SwipeEnded += HandleSwipeEnded;
        }
    }

    private void OnDisable()
    {
        if (_swipeTracker != null)
        {
            _swipeTracker.SwipeEnded -= HandleSwipeEnded;
        }
    }

    private void HandleSwipeEnded(Vector2 startScreen, Vector2 endScreen)
    {
        if (_target == null)
        {
            return;
        }
        if (_camera == null)
        {
            Debug.LogWarning($"[{nameof(DebugSlicer)}] No camera assigned and Camera.main is null.");
            return;
        }

        if (SwipePlaneBuilder.TryBuild(_camera, startScreen, endScreen, out Plane plane) == false)
        {
            Debug.LogWarning($"[{nameof(DebugSlicer)}] Failed to build cut plane (degenerate swipe).");
            return;
        }

        Debug.Log($"[{nameof(DebugSlicer)}] Slicing '{_target.name}' along plane (normal {plane.normal}).");
        _target.Slice(plane);
    }
}
