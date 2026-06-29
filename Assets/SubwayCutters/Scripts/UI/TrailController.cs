using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TrailRenderer))]
public class TrailController : MonoBehaviour
{
    [SerializeField]
    private Camera _camera;

    private TrailRenderer _trail;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();

        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    public void SwitchTrailMaterial(Material newMaterial)
    {
        _trail.material = newMaterial;
    }

    private bool _inputDown = false;
    Vector2 _inputPos = Vector2.zero;

    private void Update()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null)
        {
            _trail.emitting = false;
            return;
        }

        bool isPressed = pointer.press.isPressed;

        if (isPressed == true)
        {
            Vector2 screenPos = pointer.position.ReadValue();
            transform.position = ScreenToWorld(screenPos);
            _trail.emitting = true;
        }
        else
        {
            _trail.emitting = false;
        }
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                return transform.position;
            }
        }

        // Preserve current depth from the camera so the trail stays in the same XY plane.
        float depth = _camera.WorldToScreenPoint(transform.position).z;
        return _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
    }
}