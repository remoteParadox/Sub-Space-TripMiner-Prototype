using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Handles mouse input via raycasting from the camera.
/// Left-click: first-click setup, reveal, game over, 3D flood fill.
/// Right-click: toggles the containment shield on a block.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the GameObject that has the GridManager component here.")]
    public GridManager gridManager;
    [Tooltip("Leave empty to use Camera.main (the camera tagged MainCamera).")]
    public Camera playerCamera;
    [Tooltip("Optional. Keeps the depth slider's hidden layers in sync after reveals and resets.")]
    public DepthSliderController depthSlider;
    [Tooltip("Optional. Handles shields, Volatile Mode, scoring and the end screens.")]
    public GameManager gameManager;

    [Header("Raycast")]
    public float maxRayDistance = 500f;

    [Header("Number Labels")]
    [Tooltip("Optional. Leave empty to use Unity's built-in font.")]
    public Font labelFont;
    public float labelCharacterSize = 0.12f;
    public int labelFontSize = 64;

    [Header("Events")]
    public UnityEvent onGameOver;
    public UnityEvent onVictory;

    private bool gameStarted = false;
    private bool gameOver = false;
    private int safeBlocksTotal = 0;
    private int safeBlocksRevealed = 0;

    private readonly List<Transform> labels = new List<Transform>();

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            Debug.LogError("PlayerController: no camera assigned and no camera tagged MainCamera was found.", this);
        }

        if (gridManager == null)
        {
            Debug.LogError("PlayerController: GridManager is not assigned in the Inspector.", this);
        }
    }

    private void Update()
    {
        if (gameOver
            || (gameManager != null && !gameManager.IsPlaying)
            || gridManager == null
            || gridManager.grid == null
            || playerCamera == null)
        {
            return;
        }

        // Don't treat clicks on UI (like the depth slider) as clicks on the grid.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SubSpaceBlock block = GetBlockUnderCursor();
            if (block != null)
            {
                HandleLeftClick(block);
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            SubSpaceBlock block = GetBlockUnderCursor();
            if (block != null)
            {
                HandleRightClick(block);
            }
        }
    }

    private void LateUpdate()
    {
        // Billboard the number labels so they always face the camera.
        if (playerCamera == null) return;

        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i] != null)
            {
                labels[i].rotation = playerCamera.transform.rotation;
            }
        }
    }

    // ------------------------------------------------------------------
    // Input handling
    // ------------------------------------------------------------------

    private SubSpaceBlock GetBlockUnderCursor()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxRayDistance))
        {
            return hit.collider.GetComponentInParent<SubSpaceBlock>();
        }

        return null;
    }

    private void HandleRightClick(SubSpaceBlock block)
    {
        // Revealed blocks can't be flagged.
        if (block.isStabilized) return;

        block.ToggleShield();
    }

    private void HandleLeftClick(SubSpaceBlock block)
    {
        // Ignore clicks on revealed or shielded blocks.
        if (block.isStabilized || block.isShielded) return;

        if (!gameStarted)
        {
            StartGame(block);
        }

        if (block.isSingularity)
        {
            HandleSingularityClick(block);
            return;
        }

        int revealedBefore = safeBlocksRevealed;

        RevealWithFloodFill(block);
        RefreshDepthView();

        if (gameManager != null)
        {
            gameManager.RegisterBlocksHarvested(safeBlocksRevealed - revealedBefore);
        }

        CheckVictory();
    }

    private void HandleSingularityClick(SubSpaceBlock block)
    {
        // Without a GameManager, a singularity is an instant game over.
        if (gameManager == null)
        {
            TriggerGameOver();
            return;
        }

        bool fatal = gameManager.RegisterSingularityHit();

        if (fatal)
        {
            TriggerGameOver();
            return;
        }

        // The shields absorbed the hit: the singularity is safely stabilized.
        // Trigger chain reaction - reveal surrounding area as per plan
        StabilizeBlock(block);
        RevealWithFloodFill(block);
        RefreshDepthView();
        CheckVictory();
    }

    // ------------------------------------------------------------------
    // Game flow
    // ------------------------------------------------------------------

    private void StartGame(SubSpaceBlock firstBlock)
    {
        // SetupGame resets every block, which would wipe shields placed before
        // the first click. Remember them and re-apply afterwards.
        List<SubSpaceBlock> preShielded = new List<SubSpaceBlock>();
        foreach (SubSpaceBlock b in gridManager.grid)
        {
            if (b != null && b.isShielded)
            {
                preShielded.Add(b);
            }
        }

        gridManager.SetupGame(firstBlock.x, firstBlock.y, firstBlock.z);

        foreach (SubSpaceBlock b in preShielded)
        {
            b.ToggleShield();
        }

        int singularityTotal = 0;
        foreach (SubSpaceBlock b in gridManager.grid)
        {
            if (b != null && b.isSingularity)
            {
                singularityTotal++;
            }
        }

        safeBlocksTotal = gridManager.grid.Length - singularityTotal;
        safeBlocksRevealed = 0;
        gameStarted = true;

        if (gameManager != null)
        {
            gameManager.SetSafeBlockTotal(safeBlocksTotal);
        }
    }

    private void TriggerGameOver()
    {
        gameOver = true;
        Debug.Log("GAME OVER: singularity detonated!");

        // Expose every singularity in red.
        foreach (SubSpaceBlock b in gridManager.grid)
        {
            if (b == null || !b.isSingularity) continue;

            MeshRenderer mr = b.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = true;
                mr.material.color = Color.red;
            }
        }

        onGameOver.Invoke();
    }

    private void CheckVictory()
    {
        if (gameOver) return;

        if (gameStarted && safeBlocksRevealed >= safeBlocksTotal)
        {
            gameOver = true;
            Debug.Log("VICTORY: all safe blocks stabilized!");
            onVictory.Invoke();

            if (gameManager != null)
            {
                gameManager.TriggerVictory();
            }
        }
    }

    /// <summary>
    /// Clears all state so a new game can begin. The next left-click
    /// will generate a fresh board layout.
    /// </summary>
    public void ResetGame()
    {
        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i] != null)
            {
                Destroy(labels[i].gameObject);
            }
        }
        labels.Clear();

        if (gridManager != null && gridManager.grid != null)
        {
            foreach (SubSpaceBlock b in gridManager.grid)
            {
                if (b == null) continue;

                b.ResetState();

                foreach (Collider c in b.GetComponentsInChildren<Collider>())
                {
                    c.enabled = true;
                }
            }
        }

        gameStarted = false;
        gameOver = false;
        safeBlocksTotal = 0;
        safeBlocksRevealed = 0;

        RefreshDepthView();
    }

    private void RefreshDepthView()
    {
        if (depthSlider != null)
        {
            depthSlider.RefreshVisibility();
        }
    }

    // ------------------------------------------------------------------
    // Reveal logic
    // ------------------------------------------------------------------

    /// <summary>
    /// Stabilizes the clicked block. If it has zero adjacent singularities,
    /// spreads outward through all 26 neighbors, stabilizing every connected
    /// zero-value block and the numbered blocks bordering them.
    /// Uses an explicit queue instead of recursion, which gives the same
    /// result but cannot overflow the call stack on large grids.
    /// </summary>
    private void RevealWithFloodFill(SubSpaceBlock start)
    {
        Queue<SubSpaceBlock> queue = new Queue<SubSpaceBlock>();

        StabilizeBlock(start);
        if (start.adjacentSingularities == 0)
        {
            queue.Enqueue(start);
        }

        while (queue.Count > 0)
        {
            SubSpaceBlock current = queue.Dequeue();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;

                        int nx = current.x + dx;
                        int ny = current.y + dy;
                        int nz = current.z + dz;

                        if (!gridManager.IsInBounds(nx, ny, nz)) continue;

                        SubSpaceBlock neighbor = gridManager.grid[nx, ny, nz];

                        if (neighbor == null
                            || neighbor.isStabilized
                            || neighbor.isShielded
                            || neighbor.isSingularity)
                        {
                            continue;
                        }

                        StabilizeBlock(neighbor);

                        if (neighbor.adjacentSingularities == 0)
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Marks a block as stabilized, makes it see-through, stops it from
    /// blocking raycasts, and shows its number if it is above zero.
    /// </summary>
    private void StabilizeBlock(SubSpaceBlock block)
    {
        block.isStabilized = true;

        // A defused singularity is not one of the safe blocks that must be harvested.
        if (!block.isSingularity)
        {
            safeBlocksRevealed++;
        }

        // Hide the cube so the player can see inside the 3D grid.
        block.SetPhasedState(true);

        // Disable colliders so later clicks pass through to blocks behind it.
        foreach (Collider c in block.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        if (block.adjacentSingularities > 0)
        {
            CreateNumberLabel(block);
        }
    }

    private void CreateNumberLabel(SubSpaceBlock block)
    {
        GameObject labelObject = new GameObject("Label_" + block.adjacentSingularities);
        labelObject.transform.SetParent(block.transform, false);
        labelObject.transform.localPosition = Vector3.zero;

        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = block.adjacentSingularities.ToString();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = labelFontSize;
        textMesh.characterSize = labelCharacterSize;

        // Cyan (low) to red (high) so danger is readable at a glance.
        float t = Mathf.Clamp01(block.adjacentSingularities / 26f);
        textMesh.color = Color.Lerp(Color.cyan, Color.red, t * 2f);

        Font font = GetLabelFont();
        if (font != null)
        {
            textMesh.font = font;
            MeshRenderer labelRenderer = labelObject.GetComponent<MeshRenderer>();
            if (labelRenderer != null)
            {
                labelRenderer.sharedMaterial = font.material;
            }
        }

        labels.Add(labelObject.transform);
    }

    private Font GetLabelFont()
    {
        if (labelFont != null)
        {
            return labelFont;
        }

#if UNITY_2022_2_OR_NEWER
        labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        labelFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif

        if (labelFont == null)
        {
            labelFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
        }

        return labelFont;
    }
}