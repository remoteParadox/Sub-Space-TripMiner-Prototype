using UnityEngine;

/// <summary>
/// Attach to each block prefab. Holds the block's grid coordinates,
/// game-state flags, and simple visual-state helpers.
/// </summary>
public class SubSpaceBlock : MonoBehaviour
{
    [Header("Grid Position")]
    public int x;
    public int y;
    public int z;

    [Header("Status Flags")]
    public bool isSingularity;
    public bool isStabilized;
    public bool isShielded;

    [Header("Neighbor Data")]
    public int adjacentSingularities;

    [Header("Visuals")]
    [Tooltip("Optional. If left empty, the first MeshRenderer on this object or its children is used.")]
    [SerializeField] private MeshRenderer meshRenderer;

    private Color originalColor = Color.white;
    private bool hasCachedColor = false;

    private void Awake()
    {
        CacheRenderer();
    }

    /// <summary>
    /// Sets the block's grid coordinates and resets all state.
    /// Called by GridManager immediately after the block is spawned.
    /// </summary>
    public void Initialize(int gridX, int gridY, int gridZ)
    {
        x = gridX;
        y = gridY;
        z = gridZ;

        ResetState();
    }

    /// <summary>
    /// Clears all gameplay state and restores default visuals.
    /// </summary>
    public void ResetState()
    {
        isSingularity = false;
        isStabilized = false;
        isShielded = false;
        adjacentSingularities = 0;

        CacheRenderer();

        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
            if (Application.isPlaying) meshRenderer.material.color = originalColor;
        }
    }

    /// <summary>
    /// Toggles the shield. When shielded the block turns cyan/blue;
    /// when unshielded it returns to its original color.
    /// </summary>
    public void ToggleShield()
    {
        isShielded = !isShielded;

        CacheRenderer();

        if (meshRenderer != null)
        {
            if (Application.isPlaying) meshRenderer.material.color = isShielded ? new Color(0f, 0.8f, 1f, 1f) : originalColor;
        }
    }

    /// <summary>
    /// Hides the block's mesh when phased (inside view), shows it otherwise.
    /// For holographic effect, we can also adjust material.
    /// </summary>
    public void SetPhasedState(bool isPhased)
    {
        CacheRenderer();

        if (meshRenderer != null)
        {
            meshRenderer.enabled = !isPhased;
        }
    }

    private void CacheRenderer()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (meshRenderer != null && !hasCachedColor)
        {
            originalColor = Application.isPlaying ? meshRenderer.material.color : meshRenderer.sharedMaterial.color;
            hasCachedColor = true;
        }
    }
}