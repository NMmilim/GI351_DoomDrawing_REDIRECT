using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wave-based enemy spawner for top-down 2D games.
///
/// SETUP
///   • Place this script on an empty GameObject in your scene.
///   • The spawn position is the transform's world position.
///   • Define waves in the Inspector. Each wave has its own enemy prefab,
///     count, and spawn rate.
///   • Waves progress automatically; optionally wait for wave to be cleared
///     before the next one starts.
///   • Works alongside the CoverPoint system — just make sure CoverPoints
///     are placed near the spawner so spawned enemies can find cover.
///
/// INSPECTOR QUICK-START
///   1. Set spawnRadius so enemies appear scattered (0 = exact spawner position).
///   2. Add at least one Wave entry and assign an Enemy_Range prefab to it.
///   3. Hit Play — the spawner handles the rest.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Wave Definition
    // ─────────────────────────────────────────────────────────────

    [System.Serializable]
    public class Wave
    {
        [Tooltip("Display name shown in logs and UI (optional).")]
        public string waveName = "Wave";

        [Tooltip("Enemy prefab to spawn for this wave. Must have Enemy_Range component.")]
        public GameObject enemyPrefab;

        [Tooltip("How many enemies spawn in this wave.")]
        [Range(1, 50)]
        public int enemyCount = 3;

        [Tooltip("Seconds between each individual enemy spawn in this wave.")]
        public float spawnInterval = 1f;
    }

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Waves
    // ─────────────────────────────────────────────────────────────

    [Header("Waves")]
    [Tooltip("List of waves in order. Each wave spawns after the previous is cleared (or on timer).")]
    public Wave[] waves;

    [Tooltip("Seconds to wait between waves (shown as a countdown).")]
    public float delayBetweenWaves = 5f;

    [Tooltip("If true, wait until ALL enemies in the current wave are dead before starting the next. " +
             "If false, next wave starts after delayBetweenWaves regardless.")]
    public bool waitForWaveClear = true;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Spawn Settings
    // ─────────────────────────────────────────────────────────────

    [Header("Spawn Settings")]
    [Tooltip("Enemies spawn at a random position within this radius around the spawner. " +
             "Set to 0 to spawn at exactly the spawner's position.")]
    public float spawnRadius = 1.5f;

    [Tooltip("Seconds after the scene starts before the first wave spawns.")]
    public float initialDelay = 2f;

    [Tooltip("If true, loop waves indefinitely after the last wave is complete. " +
             "Difficulty can be scaled by adjusting enemyCount each loop.")]
    public bool loopWaves = false;

    [Tooltip("Each loop, multiply enemy count per wave by this value. " +
             "Set to 1 for no scaling.")]
    [Range(1f, 3f)]
    public float loopDifficultyMultiplier = 1.2f;

    // ─────────────────────────────────────────────────────────────
    //  Inspector – Read-Only Status (visible at runtime in Inspector)
    // ─────────────────────────────────────────────────────────────

    [Header("Status (Read-Only at Runtime)")]
    [SerializeField] private int  _currentWaveIndex = 0;
    [SerializeField] private int  _loopCount        = 0;
    [SerializeField] private int  _aliveEnemies     = 0;
    [SerializeField] private bool _spawning         = false;
    [SerializeField] private bool _allWavesDone     = false;

    // ─────────────────────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────────────────────

    /// <summary>All enemies currently alive from this spawner.</summary>
    private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();

    // ─────────────────────────────────────────────────────────────
    //  Unity Messages
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning($"[EnemySpawner] '{name}': No waves configured.");
            return;
        }

        StartCoroutine(RunSpawner());
    }

    // ─────────────────────────────────────────────────────────────
    //  Spawner Coroutine
    // ─────────────────────────────────────────────────────────────

    private IEnumerator RunSpawner()
    {
        _spawning = true;

        // Initial delay before first wave
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        do
        {
            for (int waveIdx = 0; waveIdx < waves.Length; waveIdx++)
            {
                _currentWaveIndex = waveIdx;
                Wave wave = waves[waveIdx];

                if (wave.enemyPrefab == null)
                {
                    Debug.LogWarning($"[EnemySpawner] Wave '{wave.waveName}' has no enemy prefab assigned — skipping.");
                    continue;
                }

                // How many enemies this wave (scaled by loop difficulty)
                int count = Mathf.RoundToInt(wave.enemyCount * Mathf.Pow(loopDifficultyMultiplier, _loopCount));

                Debug.Log($"[EnemySpawner] Starting {wave.waveName} — {count} enemies.");

                // Spawn enemies one by one
                for (int i = 0; i < count; i++)
                {
                    SpawnEnemy(wave.enemyPrefab);
                    if (i < count - 1)
                        yield return new WaitForSeconds(wave.spawnInterval);
                }

                // Wait for wave to be cleared and/or delay before next wave
                if (waitForWaveClear)
                {
                    // Poll until all spawned enemies are dead
                    yield return new WaitUntil(WaveIsCleared);
                }

                // Inter-wave delay (runs even if waitForWaveClear is false)
                if (waveIdx < waves.Length - 1 || loopWaves)
                {
                    Debug.Log($"[EnemySpawner] Wave {wave.waveName} complete. Next wave in {delayBetweenWaves}s.");
                    yield return new WaitForSeconds(delayBetweenWaves);
                }
            }

            _loopCount++;

        } while (loopWaves);

        _spawning     = false;
        _allWavesDone = true;
        Debug.Log($"[EnemySpawner] All waves complete.");
    }

    // ─────────────────────────────────────────────────────────────
    //  Spawning
    // ─────────────────────────────────────────────────────────────

    private void SpawnEnemy(GameObject prefab)
    {
        // Random position within spawnRadius
        Vector2 offset   = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        _spawnedEnemies.Add(enemy);
        _aliveEnemies++;

        // Notify when this enemy dies so we can update the alive count
        // We attach a death-notification component automatically
        SpawnerDeathNotifier notifier = enemy.AddComponent<SpawnerDeathNotifier>();
        notifier.Init(this);
    }

    // ─────────────────────────────────────────────────────────────
    //  Wave Clear Check
    // ─────────────────────────────────────────────────────────────

    /// <summary>Called by SpawnerDeathNotifier when an enemy from this spawner dies.</summary>
    public void OnEnemyDied()
    {
        _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
        // Also clean up null refs in the list
        _spawnedEnemies.RemoveAll(e => e == null);
    }

    private bool WaveIsCleared()
    {
        _spawnedEnemies.RemoveAll(e => e == null);
        return _spawnedEnemies.Count == 0;
    }

    // ─────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────

    /// <summary>True when the final wave has been completed (and loopWaves is false).</summary>
    public bool AllWavesDone => _allWavesDone;

    /// <summary>0-based index of the currently active wave.</summary>
    public int CurrentWave => _currentWaveIndex;

    /// <summary>Number of enemies from this spawner still alive.</summary>
    public int AliveEnemies => _aliveEnemies;

    // ─────────────────────────────────────────────────────────────
    //  Scene Gizmos
    // ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Spawn area circle
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, spawnRadius);

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // Spawner marker cross
        Gizmos.color = Color.cyan;
        float s = 0.3f;
        Gizmos.DrawLine(transform.position - Vector3.right * s, transform.position + Vector3.right * s);
        Gizmos.DrawLine(transform.position - Vector3.up    * s, transform.position + Vector3.up    * s);
    }
#endif
}

// ─────────────────────────────────────────────────────────────────────────────
//  Helper: SpawnerDeathNotifier
//  Auto-attached to each spawned enemy to call back to the spawner on death.
//  You do not need to place or configure this manually.
// ─────────────────────────────────────────────────────────────────────────────

public class SpawnerDeathNotifier : MonoBehaviour
{
    private EnemySpawner _spawner;

    public void Init(EnemySpawner spawner) => _spawner = spawner;

    private void OnDestroy()
    {
        if (_spawner != null)
            _spawner.OnEnemyDied();
    }
}
