using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects a UI Slider to the grid's Y axis so the player can peel away
/// the top layers of the cube and see the numbers inside.
///
/// Slider value N means: layers 0..N are visible, layers above N are hidden.
/// Only the layers between the old and new slider value are touched,
/// so dragging the slider stays cheap even on large grids.
/// </summary>
[DefaultExecutionOrder(100)] // Start() runs after GridManager.Start() has built the grid.
public class DepthSliderController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the GameObject that has the GridManager component here.")]
    public GridManager gridManager;
    [Tooltip("Drag the UI Slider here.")]
    public Slider depthSlider;

    private int currentLayer;
    private int maxLayer;
    private bool isInitialized;

    // Caches so we never call GetComponent on blocks while the slider moves.
    private Collider[,,] colliderCache;
    private Renderer[,,] labelCache;

    private void OnEnable()
    {
        if (depthSlider != null)
        {
            depthSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    private void OnDisable()
    {
        if (depthSlider != null)
        {
            depthSlider.onValueChanged.RemoveListener(OnSliderChanged);
        }
    }

    private void Start()
    {
        if (gridManager == null || depthSlider == null)
        {
            Debug.LogError("DepthSliderController: assign both the GridManager and the Slider in the Inspector.", this);
            return;
        }

        SyncToGrid();
    }

    /// <summary>
    /// Reads the grid's dimensions, configures the slider range (0 to max Y index),
    /// and resets the view so every layer is visible.
    /// Call this again if you ever regenerate the grid with a different size.
    /// </summary>
    public void SyncToGrid()
    {
        isInitialized = false;

        if (gridManager == null || depthSlider == null || gridManager.grid == null)
        {
            return;
        }

        SubSpaceBlock[,,] grid = gridManager.grid;
        int sizeX = grid.GetLength(0);
        int sizeY = grid.GetLength(1);
        int sizeZ = grid.GetLength(2);

        maxLayer = sizeY - 1;
        currentLayer = maxLayer;

        colliderCache = new Collider[sizeX, sizeY, sizeZ];
        labelCache = new Renderer[sizeX, sizeY, sizeZ];

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    SubSpaceBlock block = grid[x, y, z];
                    if (block != null)
                    {
                        colliderCache[x, y, z] = block.GetComponentInChildren<Collider>();
                    }
                }
            }
        }

        depthSlider.wholeNumbers = true;
        depthSlider.minValue = 0;
        depthSlider.maxValue = maxLayer;
        depthSlider.SetValueWithoutNotify(maxLayer);

        isInitialized = true;
        RefreshVisibility();
    }

    /// <summary>
    /// Re-applies the current slider depth to every block.
    /// PlayerController calls this after reveals and resets, because those
    /// operations can change blocks inside layers that are currently hidden.
    /// </summary>
    public void RefreshVisibility()
    {
        if (!isInitialized)
        {
            // The grid may not have existed at Start (e.g. Generate On Start was off).
            SyncToGrid();
            return;
        }

        ApplyLayers(0, maxLayer);
    }

    private void OnSliderChanged(float value)
    {
        if (!isInitialized) return;

        int newLayer = Mathf.Clamp(Mathf.RoundToInt(value), 0, maxLayer);
        if (newLayer == currentLayer) return;

        int oldLayer = currentLayer;
        currentLayer = newLayer;

        // Only the layers between the old and new value change state.
        int fromY = Mathf.Min(oldLayer, newLayer) + 1;
        int toY = Mathf.Max(oldLayer, newLayer);
        ApplyLayers(fromY, toY);
    }

    /// <summary>
    /// Applies visibility to every block whose Y index is within [fromY, toY].
    /// A block is visible when its Y index is at or below the current layer.
    /// </summary>
    private void ApplyLayers(int fromY, int toY)
    {
        SubSpaceBlock[,,] grid = gridManager.grid;
        int sizeX = grid.GetLength(0);
        int sizeZ = grid.GetLength(2);

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = fromY; y <= toY; y++)
            {
                bool visible = y <= currentLayer;

                for (int z = 0; z < sizeZ; z++)
                {
                    ApplyBlockVisibility(grid[x, y, z], x, y, z, visible);
                }
            }
        }
    }

    private void ApplyBlockVisibility(SubSpaceBlock block, int x, int y, int z, bool visible)
    {
        if (block == null) return;

        // Stabilized blocks are permanently phased out by the reveal logic,
        // so never touch their mesh. Only their floating number follows the slider.
        if (block.isStabilized)
        {
            SetLabelVisible(block, x, y, z, visible);
            return;
        }

        block.SetPhasedState(!visible);

        // Hidden blocks must not catch raycasts, or the player would be
        // clicking invisible blocks in the peeled-away layers.
        Collider col = colliderCache[x, y, z];
        if (col != null)
        {
            col.enabled = visible;
        }
    }

    private void SetLabelVisible(SubSpaceBlock block, int x, int y, int z, bool visible)
    {
        // Zero-value blocks have no number label.
        if (block.adjacentSingularities <= 0) return;

        Renderer labelRenderer = labelCache[x, y, z];

        if (labelRenderer == null)
        {
            TextMesh textMesh = block.GetComponentInChildren<TextMesh>();
            if (textMesh == null) return;

            labelRenderer = textMesh.GetComponent<Renderer>();
            labelCache[x, y, z] = labelRenderer;
        }

        if (labelRenderer != null)
        {
            labelRenderer.enabled = visible;
        }
    }
}