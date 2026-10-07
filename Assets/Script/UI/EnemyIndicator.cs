using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays directional indicator arrows pointing toward remaining enemies on the map.
/// Activates only when the alive enemy count drops below a configured threshold (default < 6).
///
/// FEATURES:
///   1. Zero setup required: Auto-detects Player, Camera, and active enemies.
///   2. Auto-generated Sprite: If no arrow sprite is assigned, a clean, high-res chevron
///      arrow sprite is procedurally created at runtime.
///   3. Two display modes:
///      - ScreenEdge: Arrows clamp to the edges of the screen when enemies are off-screen.
///      - AroundPlayer: Arrows form a ring orbiting around the player, pointing toward each enemy.
///   4. High performance: Object-pooled arrow renderers with zero GC allocations in LateUpdate.
///
/// HOW TO USE:
///   Attach this script to any active GameObject in your scene (e.g. Main Camera, GameManager,
///   or create an empty GameObject named "EnemyIndicators").
/// </summary>
public class EnemyIndicator : MonoBehaviour
{
    public enum IndicatorMode
    {
        [Tooltip("Arrows sit along the screen border when enemies are off-screen.")]
        ScreenEdge,

        [Tooltip("Arrows form an orbit ring around the player character pointing toward enemies.")]
        AroundPlayer
    }

    // -----------------------------------------------------------------
    //  Inspector - Trigger & Mode Settings
    // -----------------------------------------------------------------

    [Header("Trigger Threshold")]
    [Tooltip("Arrows activate only when the number of alive enemies is strictly LESS than this number (e.g. 6 = triggers when 5 or fewer enemies remain).")]
    [Range(1, 20)]
    public int enemyThreshold = 6;

    [Header("Display Mode")]
    [Tooltip("How indicators are placed relative to the player's view.")]
    public IndicatorMode mode = IndicatorMode.ScreenEdge;

    [Tooltip("When using ScreenEdge mode: hide the arrow if the enemy is already visible inside the camera view?")]
    public bool hideWhenOnScreen = true;

    [Tooltip("Offset in world units above the enemy when hideWhenOnScreen is false and the enemy is on-screen.")]
    public float onScreenHoverHeight = 1.2f;

    // -----------------------------------------------------------------
    //  Inspector - Layout & Positioning
    // -----------------------------------------------------------------

    [Header("Screen Edge Mode Settings")]
    [Tooltip("Margin from the viewport border in world units (higher = arrows sit further inside the screen).")]
    public float screenMargin = 0.8f;

    [Header("Around Player Mode Settings")]
    [Tooltip("Radius of the arrow orbit ring around the player (in world units).")]
    public float playerOrbitRadius = 2.2f;

    // -----------------------------------------------------------------
    //  Inspector - Visuals
    // -----------------------------------------------------------------

    [Header("Visuals")]
    [Tooltip("Optional custom arrow sprite. If left empty, a sharp procedural neon chevron is created automatically.")]
    public Sprite customArrowSprite;

    [Tooltip("Color tint of the indicator arrows.")]
    public Color arrowColor = new Color(1f, 0.45f, 0.1f, 0.95f);

    [Tooltip("Base size/scale of the indicator arrows.")]
    public float arrowScale = 0.65f;

    [Tooltip("Sorting layer name for the arrow sprites.")]
    public string sortingLayerName = "Default";

    [Tooltip("Sorting order for the arrow sprites (higher = rendered in front of map & characters).")]
    public int sortingOrder = 1000;

    [Header("Pulse Animation")]
    [Tooltip("Smoothly pulse arrow scale so it easily catches the player's attention.")]
    public bool enablePulse = true;

    [Tooltip("Frequency of the scale pulse.")]
    public float pulseSpeed = 4f;

    [Tooltip("Magnitude of the scale pulse (+/- fraction of arrowScale).")]
    [Range(0f, 0.5f)]
    public float pulseAmount = 0.15f;

    // -----------------------------------------------------------------
    //  Inspector - Optional Explicit References
    // -----------------------------------------------------------------

    [Header("References (Optional - Auto-detected if null)")]
    [Tooltip("Player transform. If left null, automatically finds GameObject tagged 'Player'.")]
    public Transform player;

    [Tooltip("Target camera. If left null, uses Camera.main.")]
    public Camera targetCamera;

    // -----------------------------------------------------------------
    //  Internal State & Pooling
    // -----------------------------------------------------------------

    private static Sprite _proceduralSprite;
    private readonly List<IndicatorEntry> _pool = new List<IndicatorEntry>();
    private readonly List<Transform> _aliveEnemies = new List<Transform>(16);

    private class IndicatorEntry
    {
        public GameObject gameObject;
        public Transform transform;
        public SpriteRenderer renderer;
    }

    // -----------------------------------------------------------------
    //  Unity Messages
    // -----------------------------------------------------------------

    private void Awake()
    {
        EnsureSpriteCreated();
    }

    private void Start()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        // 1. Resolve camera and player if missing
        if (targetCamera == null) targetCamera = Camera.main;
        if (player == null) ResolvePlayer();

        // 2. Collect alive enemies
        CollectAliveEnemies();

        int count = _aliveEnemies.Count;

        // 3. Condition check: only show when alive count is lower than threshold and > 0
        bool shouldShow = (count > 0 && count < enemyThreshold);

        if (!shouldShow || targetCamera == null)
        {
            DisableAllIndicators();
            return;
        }

        // 4. Update an indicator for each alive enemy
        for (int i = 0; i < count; i++)
        {
            Transform enemy = _aliveEnemies[i];
            if (enemy == null) continue;

            IndicatorEntry entry = GetOrCreateEntry(i);
            UpdateIndicator(entry, enemy.position);
        }

        // 5. Hide any unused indicators in the pool
        for (int i = count; i < _pool.Count; i++)
        {
            if (_pool[i].gameObject.activeSelf)
                _pool[i].gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        DisableAllIndicators();
    }

    // -----------------------------------------------------------------
    //  Enemy Tracking
    // -----------------------------------------------------------------

    private void CollectAliveEnemies()
    {
        _aliveEnemies.Clear();

        // 1. Primary source: Enemy_Range.Active static list
        if (Enemy_Range.Active != null)
        {
            for (int i = 0; i < Enemy_Range.Active.Count; i++)
            {
                var enemy = Enemy_Range.Active[i];
                if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
                {
                    Transform t = enemy.transform;
                    if (!_aliveEnemies.Contains(t)) _aliveEnemies.Add(t);
                }
            }
        }

        // 2. Fallback: if Active is empty, search by Enemy tag or Enemy_Range in scene
        if (_aliveEnemies.Count == 0)
        {
            var tagged = GameObject.FindGameObjectsWithTag("Enemy");
            for (int i = 0; i < tagged.Length; i++)
            {
                var go = tagged[i];
                if (go != null && go.activeInHierarchy)
                {
                    var er = go.GetComponent<Enemy_Range>();
                    if (er == null || !er.IsDead)
                    {
                        if (!_aliveEnemies.Contains(go.transform)) _aliveEnemies.Add(go.transform);
                    }
                }
            }
        }
    }

    // -----------------------------------------------------------------
    //  Indicator Positioning & Math
    // -----------------------------------------------------------------

    private void UpdateIndicator(IndicatorEntry entry, Vector3 enemyPos)
    {
        Vector3 camPos = targetCamera.transform.position;
        float playerZ = player != null ? player.position.z : 0f;

        Vector3 targetPos = Vector3.zero;
        float angleDeg = 0f;
        bool visible = true;

        if (mode == IndicatorMode.AroundPlayer && player != null)
        {
            // Mode: Orbit ring around player
            Vector2 diff = (Vector2)enemyPos - (Vector2)player.position;
            if (diff.sqrMagnitude < 0.001f) diff = Vector2.up;

            Vector2 dir = diff.normalized;
            targetPos = player.position + new Vector3(dir.x, dir.y, 0f) * playerOrbitRadius;
            targetPos.z = playerZ;
            angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }
        else
        {
            // Mode: ScreenEdge
            float orthoH = targetCamera.orthographicSize;
            float orthoW = orthoH * targetCamera.aspect;

            float margin = Mathf.Max(0.1f, screenMargin);
            float halfW = Mathf.Max(0.2f, orthoW - margin);
            float halfH = Mathf.Max(0.2f, orthoH - margin);

            // Check if enemy is already on-screen
            bool isOnScreen = Mathf.Abs(enemyPos.x - camPos.x) <= (orthoW - 0.2f) &&
                              Mathf.Abs(enemyPos.y - camPos.y) <= (orthoH - 0.2f);

            if (isOnScreen)
            {
                if (hideWhenOnScreen)
                {
                    visible = false;
                }
                else
                {
                    // Hover above enemy pointing downward
                    targetPos = enemyPos + Vector3.up * onScreenHoverHeight;
                    targetPos.z = playerZ;
                    angleDeg = -90f;
                }
            }
            else
            {
                // Ray from camera center toward enemy
                Vector2 diff = new Vector2(enemyPos.x - camPos.x, enemyPos.y - camPos.y);
                if (diff.sqrMagnitude < 0.001f) diff = Vector2.up;

                Vector2 dir = diff.normalized;

                // Intersect ray with screen edge bounding box [-halfW, halfW] x [-halfH, halfH]
                float tX = Mathf.Abs(dir.x) > 0.0001f ? halfW / Mathf.Abs(dir.x) : float.MaxValue;
                float tY = Mathf.Abs(dir.y) > 0.0001f ? halfH / Mathf.Abs(dir.y) : float.MaxValue;
                float t = Mathf.Min(tX, tY);

                targetPos = new Vector3(camPos.x + dir.x * t, camPos.y + dir.y * t, playerZ);
                angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }
        }

        if (!visible)
        {
            if (entry.gameObject.activeSelf) entry.gameObject.SetActive(false);
            return;
        }

        if (!entry.gameObject.activeSelf) entry.gameObject.SetActive(true);

        entry.transform.position = targetPos;
        entry.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        // Apply scale with pulse animation
        float currentScale = arrowScale;
        if (enablePulse)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            currentScale *= pulse;
        }

        entry.transform.localScale = new Vector3(currentScale, currentScale, 1f);

        // Ensure appearance matches inspector settings
        if (entry.renderer != null)
        {
            entry.renderer.color = arrowColor;
            entry.renderer.sortingLayerName = sortingLayerName;
            entry.renderer.sortingOrder = sortingOrder;
        }
    }

    // -----------------------------------------------------------------
    //  Pooling & Visual Setup
    // -----------------------------------------------------------------

    private IndicatorEntry GetOrCreateEntry(int index)
    {
        while (_pool.Count <= index)
        {
            GameObject go = new GameObject($"EnemyIndicator_Arrow_{_pool.Count}");
            go.transform.SetParent(transform, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = (customArrowSprite != null) ? customArrowSprite : _proceduralSprite;
            sr.color = arrowColor;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;

            IndicatorEntry entry = new IndicatorEntry
            {
                gameObject = go,
                transform = go.transform,
                renderer = sr
            };

            _pool.Add(entry);
        }

        return _pool[index];
    }

    private void DisableAllIndicators()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i].gameObject != null && _pool[i].gameObject.activeSelf)
                _pool[i].gameObject.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (player != null) return;

        if (PlayerManagement.Instance != null)
        {
            player = PlayerManagement.Instance.transform;
            return;
        }

        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null) player = playerGO.transform;
    }

    // -----------------------------------------------------------------
    //  Procedural Arrow Sprite Generation
    // -----------------------------------------------------------------

    private void EnsureSpriteCreated()
    {
        if (customArrowSprite != null || _proceduralSprite != null) return;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color white = Color.white;
        Color outlineColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Arrow pointing along +X (Right)
        Vector2 tip = new Vector2(size - 6f, size * 0.5f);
        Vector2 topWing = new Vector2(10f, size - 8f);
        Vector2 botWing = new Vector2(10f, 8f);
        Vector2 notch = new Vector2(24f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                int idx = y * size + x;

                if (PointInChevron(p, topWing, tip, botWing, notch))
                {
                    pixels[idx] = white;
                }
                else if (PointNearChevron(p, topWing, tip, botWing, notch, 2.0f))
                {
                    // Clean dark outline around the arrow
                    pixels[idx] = outlineColor;
                }
                else
                {
                    pixels[idx] = clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        _proceduralSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
        _proceduralSprite.name = "Procedural_EnemyIndicator_Arrow";
    }

    private static bool PointInChevron(Vector2 p, Vector2 top, Vector2 tip, Vector2 bot, Vector2 notch)
    {
        return PointInTriangle(p, top, tip, notch) || PointInTriangle(p, bot, notch, tip);
    }

    private static bool PointNearChevron(Vector2 p, Vector2 top, Vector2 tip, Vector2 bot, Vector2 notch, float distance)
    {
        // Simple 4-point dilation check for a crisp 1-2px border
        return PointInChevron(p + new Vector2(distance, 0), top, tip, bot, notch) ||
               PointInChevron(p - new Vector2(distance, 0), top, tip, bot, notch) ||
               PointInChevron(p + new Vector2(0, distance), top, tip, bot, notch) ||
               PointInChevron(p - new Vector2(0, distance), top, tip, bot, notch);
    }

    private static bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float s = (p0.x - p2.x) * (p.y - p2.y) - (p0.y - p2.y) * (p.x - p2.x);
        float t = (p1.x - p0.x) * (p.y - p0.y) - (p1.y - p0.y) * (p.x - p0.x);

        if ((s < 0) != (t < 0) && s != 0 && t != 0) return false;

        float d = (p2.x - p1.x) * (p.y - p1.y) - (p2.y - p1.y) * (p.x - p1.x);
        return d == 0 || (d < 0) == (s + t <= 0);
    }
}
