using UnityEngine;

[RequireComponent(typeof(Sliceable))]
public class Bomb : MonoBehaviour
{
    private Sliceable _sliceable;

    private void Awake()
    {
        _sliceable = GetComponent<Sliceable>();
    }

    private void OnEnable()
    {
        _sliceable.Sliced += HandleSliced;
    }

    private void OnDisable()
    {
        _sliceable.Sliced -= HandleSliced;
    }

    private void HandleSliced(Sliceable sliceable, Plane plane)
    {
        GameState.TriggerGameOver();
    }
}