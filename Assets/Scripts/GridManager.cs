using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the global 3D board: spawns blocks, places singularities
/// (keeping the first click safe), and computes neighbor counts.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Prefab & Layout")]
    public SubSpaceBlock blockPrefab;
    public Vector3Int gridSize = new Vector3Int(5, 5, 5);
    [Tooltip("Distance between block centers in world units.")]
    public float spacing = 1.1f;

    [Header("Game Settings")]
    public int singularityCount = 15;
    [Tooltip("If true, the first-click block AND all its 26 neighbors are kept free of singularities.")]
    public bool protectFirstClickNeighbors = false;
    public bool generateOnStart = true;

    /// <summary>The 3D array of all blocks, indexed [x, y, z].</summary>
    public SubSpaceBlock[,,] grid;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateGrid();
        }
    }

    private void OnValidate()
    {
        gridSize.x = Mathf.Max(1, gridSize.x);
        gridSize.y = Mathf.Max(1, gridSize.y);
        gridSize.z = Mathf.Max(1, gridSize.z);
        singularityCount = Mathf.Max(0, singularityCount);
        spacing = Mathf.Max(0.01f, spacing);
    }

    /// <summary>
    /// Spawns a cube array of blocks centered on the origin (0,0,0).
    /// </summary>
    public void GenerateGrid()
    {
        if (blockPrefab == null)
        {
            Debug.LogError("GridManager: blockPrefab is not assigned.", this);
            return;
        }

        ClearGrid();

        int sizeX = gridSize.x;
        int sizeY = gridSize.y;
        int sizeZ = gridSize.z;

        grid = new SubSpaceBlock[sizeX, sizeY, sizeZ];

        // Offset so the midpoint of the whole grid sits at (0,0,0).
        Vector3 offset = new Vector3(
            (sizeX - 1) * 0.5f,
            (sizeY - 1) * 0.5f,
            (sizeZ - 1) * 0.5f
        );

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    Vector3 localPos = new Vector3(
                        (x - offset.x) * spacing,
                        (y - offset.y) * spacing,
                        (z - offset.z) * spacing
                    );

                    // Parent to this transform; grid is centered on the manager's position.
                    Vector3 worldPos = transform.position + localPos;

                    SubSpaceBlock block = Instantiate(blockPrefab, worldPos, Quaternion.identity, transform);
                    block.name = "Block_" + x + "_" + y + "_" + z;
                    block.Initialize(x, y, z);

                    grid[x, y, z] = block;
                }
            }
        }
    }

    /// <summary>
    /// Places singularities randomly, guaranteeing (startX, startY, startZ) is safe,
    /// then calculates all neighbor counts. Call this on the player's first click.
    /// </summary>
    public void SetupGame(int startX, int startY, int startZ)
    {
        if (grid == null)
        {
            GenerateGrid();
            if (grid == null) return;
        }

        if (!IsInBounds(startX, startY, startZ))
        {
            Debug.LogError("GridManager.SetupGame: start coordinates (" + startX + ", " + startY + ", " + startZ + ") are out of bounds.", this);
            return;
        }

        // Reset every block's state (in case of a restart).
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    grid[x, y, z].ResetState();
                }
            }
        }

        // Build the list of cells eligible to hold a singularity.
        List<Vector3Int> candidates = new List<Vector3Int>();

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    if (IsProtectedCell(x, y, z, startX, startY, startZ))
                    {
                        continue;
                    }
                    candidates.Add(new Vector3Int(x, y, z));
                }
            }
        }

        int toPlace = Mathf.Min(singularityCount, candidates.Count);
        if (toPlace < singularityCount)
        {
            Debug.LogWarning("GridManager: requested " + singularityCount + " singularities but only " + toPlace + " cells are available. Placing " + toPlace + ".", this);
        }

        // Partial Fisher-Yates shuffle: pick 'toPlace' unique cells.
        for (int i = 0; i < toPlace; i++)
        {
            int j = Random.Range(i, candidates.Count);
            Vector3Int temp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temp;

            Vector3Int pos = candidates[i];
            grid[pos.x, pos.y, pos.z].isSingularity = true;
        }

        CalculateAdjacentSingularities();
    }

    /// <summary>
    /// For every block, counts singularities among its (up to) 26 neighbors
    /// with strict boundary checks, and stores the result.
    /// </summary>
    public void CalculateAdjacentSingularities()
    {
        if (grid == null) return;

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    int count = 0;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                // Skip the block itself.
                                if (dx == 0 && dy == 0 && dz == 0) continue;

                                int nx = x + dx;
                                int ny = y + dy;
                                int nz = z + dz;

                                if (!IsInBounds(nx, ny, nz)) continue;

                                if (grid[nx, ny, nz].isSingularity)
                                {
                                    count++;
                                }
                            }
                        }
                    }

                    grid[x, y, z].adjacentSingularities = count;
                }
            }
        }
    }

    /// <summary>
    /// Returns true if the coordinates fall inside the grid.
    /// </summary>
    public bool IsInBounds(int x, int y, int z)
    {
        return x >= 0 && x < gridSize.x
            && y >= 0 && y < gridSize.y
            && z >= 0 && z < gridSize.z;
    }

    private bool IsProtectedCell(int x, int y, int z, int startX, int startY, int startZ)
    {
        if (protectFirstClickNeighbors)
        {
            return Mathf.Abs(x - startX) <= 1
                && Mathf.Abs(y - startY) <= 1
                && Mathf.Abs(z - startZ) <= 1;
        }

        return x == startX && y == startY && z == startZ;
    }

    private void ClearGrid()
    {
        if (grid == null) return;

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                for (int z = 0; z < grid.GetLength(2); z++)
                {
                    if (grid[x, y, z] != null)
                    {
                        Destroy(grid[x, y, z].gameObject);
                    }
                }
            }
        }

        grid = null;
    }
}