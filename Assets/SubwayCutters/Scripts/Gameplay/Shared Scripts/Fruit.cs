using UnityEngine;

[RequireComponent(typeof(Sliceable))]
public class Fruit : MonoBehaviour
{
    [SerializeField, Tooltip("How many points this fruit awards when sliced.")]
    private int _scoreValue = 1;

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
        GameState.AddScore(_scoreValue);
    }
}