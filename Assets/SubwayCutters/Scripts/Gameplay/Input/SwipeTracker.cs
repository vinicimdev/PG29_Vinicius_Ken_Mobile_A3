using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeTracker : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField, Tooltip("")]
    private float _minSwipeDistancePixels = 30f;

    [SerializeField, Tooltip("")]
    private float _instantDirectionMinDelta = 2f;

    [Header("Debug")]
    [SerializeField, Tooltip("")]
    private bool _drawDebugLine = true;

    public event Action<Vector2, Vector2> SwipeEnded;

    private bool _isSwiping;
    private Vector2 _swipeStartScreen;
    private Vector2 _currentScreen;
    private Vector2 _previousScreen;
    private Vector2 _instantDirection = Vector2.right;

    /// <summary>Most recent per-frame swipe direction (normalized).</summary>
    public Vector2 InstantDirection => _instantDirection;
    /// <summary>True while the pointer is held past the start frame.</summary>
    public bool IsSwiping => _isSwiping;
    /// <summary>Where the current swipe began, in screen pixels.</summary>
    public Vector2 SwipeStartScreen => _swipeStartScreen;
    /// <summary>Current pointer position in screen pixels (valid while IsSwiping).</summary>
    public Vector2 CurrentScreen => _currentScreen;

    private void Update()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null)
        {
            return;
        }

        bool isPressed = pointer.press.isPressed;
        Vector2 currentPos = pointer.position.ReadValue();

        if (isPressed && _isSwiping == false)
        {
            BeginSwipe(currentPos);
        }
        else if (isPressed && _isSwiping == true)
        {
            UpdateSwipe(currentPos);
        }
        else if (isPressed == false && _isSwiping == true)
        {
            EndSwipe(currentPos);
        }
    }

    private void BeginSwipe(Vector2 startScreen)
    {
        _isSwiping = true;
        _swipeStartScreen = startScreen;
        _currentScreen = startScreen;
        _previousScreen = startScreen;
        _instantDirection = Vector2.right;

        Debug.Log($"[Swipe] Begin at screen {startScreen}");
    }

    private void UpdateSwipe(Vector2 currentPos)
    {
        _currentScreen = currentPos;

        Vector2 frameDelta = _currentScreen - _previousScreen;

        if (frameDelta.sqrMagnitude > _instantDirectionMinDelta * _instantDirectionMinDelta)
        {
            _instantDirection = frameDelta.normalized;
        }

        _previousScreen = _currentScreen;

        if (_drawDebugLine == true)
        {
            Debug.DrawLine(ScreenToWorldDebug(_swipeStartScreen), ScreenToWorldDebug(_currentScreen), Color.green);
        }
    }

    private void EndSwipe(Vector2 endScreen)
    {
        _isSwiping = false;
        _currentScreen = endScreen;

        Vector2 totalDelta = _currentScreen - _swipeStartScreen;
        float totalLength = totalDelta.magnitude;

        if (totalLength < _minSwipeDistancePixels)
        {
            Debug.Log($"[Swipe] End, ignored (only {totalLength:F1}px, below threshold).");
            return;
        }

        float overallAngle = Mathf.Atan2(totalDelta.y, totalDelta.x) * Mathf.Rad2Deg;
        float instantAngle = Mathf.Atan2(_instantDirection.y, _instantDirection.x) * Mathf.Rad2Deg;

        Debug.Log($"[Swipe] End. Length: {totalLength:F1}px | Overall angle: {overallAngle:F1}° | Last-frame angle: {instantAngle:F1}°");

        SwipeEnded?.Invoke(_swipeStartScreen, _currentScreen);
    }

    private Vector3 ScreenToWorldDebug(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return Vector3.zero;
        }

        Vector3 world = new Vector3(screenPos.x, screenPos.y, 5f);
        return cam.ScreenToWorldPoint(world);
    }
}