using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField, Tooltip("")]
    private List<GameObject> _fruitPrefabs;
    [SerializeField, Tooltip("")]
    private List<GameObject> _bombPrefabs;

    [Header("Spawn Position")]
    [SerializeField, Tooltip("")]
    private Transform _spawnPoint;
    [SerializeField, Tooltip("")]
    private float _horizontalSpread = 3f;

    [Header("Launch")]
    [SerializeField, Tooltip("")]
    private float _minLaunchSpeed = 10f;
    [SerializeField, Tooltip("")]
    private float _maxLaunchSpeed = 14f;
    [SerializeField, Tooltip("")]
    private float _horizontalSpeedSpread = 3f;
    [SerializeField, Tooltip("")]
    private float _angularSpeed = 3f;

    [Header("Timing")]
    [SerializeField, Tooltip("")]
    private float _initialDelay = 1.5f;
    [SerializeField] private float _minSpawnInterval = 0.8f;
    [SerializeField] private float _maxSpawnInterval = 1.6f;

    [Header("Bomb Chance")]
    [Range(0f, 1f)]
    [SerializeField, Tooltip("")]
    private float _bombProbability = 0.15f;

    [Header("Cleanup")]
    [SerializeField, Tooltip("")]
    private float _spawnedLifetime = 8f;

    private Coroutine _spawnLoop;

    private void OnEnable()
    {
        GameState.GameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameState.GameOver -= HandleGameOver;
    }

    public void StartSpawning()
    {
        if (_spawnLoop != null)
        {
            return;
        }
        _spawnLoop = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (_spawnLoop != null)
        {
            StopCoroutine(_spawnLoop);
            _spawnLoop = null;
        }
    }

    private void HandleGameOver()
    {
        StopSpawning();
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(_initialDelay);

        while (true)
        {
            SpawnOne();
            float wait = Random.Range(_minSpawnInterval, _maxSpawnInterval);
            yield return new WaitForSeconds(wait);
        }
    }

    private void SpawnOne()
    {
        bool spawnBomb = Random.value < _bombProbability;
        List<GameObject> pool = spawnBomb ? _bombPrefabs : _fruitPrefabs;

        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning($"[Spawner] No {(spawnBomb ? "bomb" : "fruit")} prefabs assigned, skipping spawn.", this);
            return;
        }

        GameObject prefab = pool[Random.Range(0, pool.Count)];

        Transform basePoint = _spawnPoint != null ? _spawnPoint : transform;
        Vector3 spawnPos = basePoint.position + new Vector3(Random.Range(-_horizontalSpread, _horizontalSpread), 0f, 0f);

        GameObject instance = Instantiate(prefab, spawnPos, Random.rotation);

        Rigidbody rb = instance.GetComponent<Rigidbody>();

        if (rb != null)
        {
            float upward = Random.Range(_minLaunchSpeed, _maxLaunchSpeed);
            float lateral = Random.Range(-_horizontalSpeedSpread, _horizontalSpeedSpread);
            rb.linearVelocity = new Vector3(lateral, upward, 0f);
            rb.angularVelocity = Random.insideUnitSphere * _angularSpeed;
        }
        else
        {
            Debug.LogWarning($"[Spawner] Prefab '{prefab.name}' has no Rigidbody — it won't fly.", this);
        }

        if (_spawnedLifetime > 0f)
        {
            Destroy(instance, _spawnedLifetime);
        }
    }
}