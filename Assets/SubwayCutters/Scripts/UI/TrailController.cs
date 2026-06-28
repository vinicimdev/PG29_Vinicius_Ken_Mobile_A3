using System;
using UnityEngine;

public class TrailController : MonoBehaviour
{
    private TrailRenderer _trail;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
    }

    public void SwitchTrailMaterial(Material newMaterial)
    {
        _trail.material = newMaterial;
    }

    private bool _inputDown = false;
    Vector2 _inputPos = Vector2.zero;
    
    private void Update()
    {
#if UNITY_EDITOR
        _inputPos = Input.mousePosition;
        _inputDown = Input.GetMouseButton(0);
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            inputPos = Input.GetTouch(0).position;
            inputBegan = true;
        }
#endif
        
        if (_inputDown)
        {
            Vector3 worldPos = TouchToWorldSpace(_inputPos);
            transform.position = worldPos;
        }
    }
    
    Vector3 TouchToWorldSpace(Vector2 screenPos)
    {
        Vector3 screenWithDepth = new Vector3(screenPos.x, screenPos.y, Camera.main.WorldToScreenPoint(transform.position).z);
        return Camera.main.ScreenToWorldPoint(screenWithDepth);
    }
}