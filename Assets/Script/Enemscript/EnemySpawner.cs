using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;   // A* Pathfinding Project — for AstarPath.active.GetNearest()

/// <summary>
/// Wave-based enemy spawner for top-down 2D games.
///
/// SETUP
///   1. Place this script on an empty GameObject.
///   2. Set spawnRadius so enemies appear scattered around the spawner.
///   3. Add at least one Wave entry and assign an Enemy_Range prefab.
///   4. Hit Play — the spawner handles the rest.
///
/// NOTE: Spawn positions are snapped to the nearest walkable A* graph node,
///       so enemies always start on valid pathfinding ground.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // -----------------------------------------------------------------
    //  Wave Definition
    // -----------------------------------------------------------------

    [System.Serializable]
    public class Wave
    {
        [Tooltip("Display name shown in logs (optional).")]
        public string waveName = "Wave";

        [Tooltip("Enemy prefab to spawn. Must have Enemy_Range and AIPath components.")]
        public GameObject enemyPrefab;

        [Tooltip("How many enemies spawn in this wave.")]
        [Range(1, 50)]
        public int enemyCount = 3;

        [Tooltip("Seconds between each individual enemy spawn.")]
        public float spawnInterval = 1f;
    }

    // -----------------------------------------------------------------
    //  Inspector - Waves
    // -----------------------------------------------------------------

    [Header("Waves")]
    public Wave[] waves;

    [Tooltip("Seconds to wait between waves.")]
    public float delayBetweenWaves = 5f;

    [Tooltip("If true, wait until ALL enemies are dead before starting next wave.")]
    public bool waitForWaveClear = true;

    [Tooltip("Show the 'Select Upgrade' screen (UpgradeManager) after each cleared wave. " +
             "Needs waitForWaveClear = true.")]
    public bool offerUpgradesBetweenWaves = true;

    // -----------------------------------------------------------------
    //  Inspector - Spawn Settings
    // -----------------------------------------------------------------

    [Header("Spawn Settings")]
    [Tooltip("List of transforms to spawn enemies at. If empty, uses the spawner's own position.")]
    public Transform[] spawnPoints;

    [Tooltip("Enemies appear at a random position within this radius of the chosen spawn point.")]
    public float spawnRadius = 1.5f;

    [Tooltip("Seconds after scene start before the first wave spawns.")]
    public float initialDelay = 2f;

    [Tooltip("If true, loop waves indefinitely after the last wave completes.")]
    public bool loopWaves = false;

    [Tooltip("Each loop, multiply enemy count per wave by this value.")]
    [Range(1f, 3f)]
    public float loopDifficultyMultiplier = 1.2f;

    // -----------------------------------------------------------------
    //  Inspector - Read-Only Status
    // -----------------------------------------------------------------

    [Header("Status (Read-Only at Runtime)")]
    [SerializeField] private int  _currentWaveIndex = 0;
    [SerializeField] private int  _loopCount        = 0;
    [SerializeField] private int  _aliveEnemies     = 0;
    [SerializeField] private bool _spawning         = false;
    [SerializeField] private bool _allWavesDone     = false;

    // -----------------------------------------------------------------
    //  Private
    // -----------------------------------------------------------------

    private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();

    // -----------------------------------------------------------------
    //  Unity Messages
    // -----------------------------------------------------------------

    private void Start()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning($"[EnemySpawner] '{name}': No waves configured.");
            return;
        }

        StartCoroutine(RunSpawner());
    }

    // -----------------------------------------------------------------
    //  Spawner Coroutine
    // -----------------------------------------------------------------

    private IEnumerator RunSpawner()
    {
        _spawning = true;

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
                    Debug.LogWarning($"[EnemySpawner] Wave '{wave.waveName}' has no prefab — skipping.");
                    continue;
                }

                int count = Mathf.RoundToInt(wave.enemyCount * Mathf.Pow(loopDifficultyMultiplier, _loopCount));
                Debug.Log($"[EnemySpawner] Starting {wave.waveName} — {count} enemies.");

                for (int i = 0; i < count; i++)
                {
                    SpawnEnemy(wave.enemyPrefab);
                    if (i < count - 1)
                        yield return new WaitForSeconds(wave.spawnInterval);
                }

                if (waitForWaveClear)
                    yield return new WaitUntil(WaveIsCleared);

                bool hasNextWave = waveIdx < waves.Length - 1 || loopWaves;

                // ---- Wave cleared: coins + upgrade pick -------------------
                if (waitForWaveClear)
                {
                    int clearedNumber = _loopCount * waves.Length + waveIdx + 1;   // 1-based, keeps counting across loops
                    if (GameManager.Instance != null) GameManager.Instance.OnWaveCleared(clearedNumber);

                    // Game pauses while the screen is open; WaitUntil still runs at timeScale 0.
                    if (offerUpgradesBetweenWaves && hasNextWave && UpgradeManager.Instance != null
                        && UpgradeManager.Instance.ShowChoices(clearedNumber))
                    {
                        yield return new WaitUntil(() => UpgradeManager.Instance == null || !UpgradeManager.Instance.IsOpen);
                    }
                }

                if (hasNextWave)
                {
                    Debug.Log($"[EnemySpawner] {wave.waveName} complete. Next in {delayBetweenWaves}s.");
                    yield return new WaitForSeconds(delayBetweenWaves);
                }
            }

            _loopCount++;

        } while (loopWaves);

        _spawning     = false;
        _allWavesDone = true;
        Debug.Log("[EnemySpawner] All waves complete.");
    }

    // -----------------------------------------------------------------
    //  Spawning
    // -----------------------------------------------------------------

    private void SpawnEnemy(GameObject prefab)
    {
        Transform spawnOrigin = transform;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Pick a random spawn point that isn't null
            var validPoints = new List<Transform>();
            foreach(var p in spawnPoints) if (p != null) validPoints.Add(p);
            
            if (validPoints.Count > 0)
                spawnOrigin = validPoints[Random.Range(0, validPoints.Count)];
        }

        // Random 2D position within spawnRadius
        Vector2 offset   = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = spawnOrigin.position + new Vector3(offset.x, offset.y, 0f);

        // Snap to the nearest walkable A* graph node so the enemy always
        // starts on valid pathfinding ground — no coordinate remapping needed.
        if (AstarPath.active != null)
        {
            NNInfo info = AstarPath.active.GetNearest(spawnPos, NNConstraint.Default);
            if (info.node != null)
                spawnPos = (Vector3)info.node.position;
        }

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        _spawnedEnemies.Add(enemy);
        _aliveEnemies++;

        SpawnerDeathNotifier notifier = enemy.AddComponent<SpawnerDeathNotifier>();
        notifier.Init(this);
    }

    // -----------------------------------------------------------------
    //  Wave Clear Check
    // -----------------------------------------------------------------

    public void OnEnemyDied()
    {
        _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
        _spawnedEnemies.RemoveAll(e => e == null);
    }

    private bool WaveIsCleared()
    {
        _spawnedEnemies.RemoveAll(e => e == null);
        return _spawnedEnemies.Count == 0;
    }

    // -----------------------------------------------------------------
    //  Public API
    // -----------------------------------------------------------------

    public bool AllWavesDone  => _allWavesDone;
    public int  CurrentWave   => _currentWaveIndex;
    public int  AliveEnemies  => _aliveEnemies;

    // -----------------------------------------------------------------
    //  Scene Gizmos
    // -----------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            foreach (Transform t in spawnPoints)
            {
                if (t != null) DrawSpawnGizmo(t.position);
            }
        }
        else
        {
            DrawSpawnGizmo(transform.position);
        }
    }

    private void DrawSpawnGizmo(Vector3 pos)
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.3f);
        Gizmos.DrawSphere(pos, spawnRadius);
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(pos, spawnRadius);
        Gizmos.color = Color.cyan;
        float s = 0.3f;
        Gizmos.DrawLine(pos - Vector3.right * s, pos + Vector3.right * s);
        Gizmos.DrawLine(pos - Vector3.up    * s, pos + Vector3.up    * s);
    }
#endif
}

// ─────────────────────────────────────────────────────────────────────────────
//  Helper: SpawnerDeathNotifier
// ─────────────────────────────────────────────────────────────────────────────

public class SpawnerDeathNotifier : MonoBehaviour
{
    private EnemySpawner _spawner;
    public void Init(EnemySpawner spawner) => _spawner = spawner;
    private void OnDestroy() { if (_spawner != null) _spawner.OnEnemyDied(); }
}
