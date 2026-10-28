using UnityEngine;
using UnityEngine.UI;

public class UISetupHelper : MonoBehaviour
{
    [Header("Quick Setup")]
    public bool autoSetupOnStart = true;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupUI();
        }
    }
    
    public void SetupUI()
    {
        // Find all necessary components
        GridManager gm = FindObjectOfType<GridManager>();
        PlayerController pc = FindObjectOfType<PlayerController>();
        GameManager gameMgr = FindObjectOfType<GameManager>();
        DepthSliderController dsc = FindObjectOfType<DepthSliderController>();
        CameraOrbit co = FindObjectOfType<CameraOrbit>();
        Camera cam = Camera.main;
        
        // Setup references if missing
        if (pc != null && gm != null && pc.gridManager == null)
            pc.gridManager = gm;
        if (pc != null && cam != null && pc.playerCamera == null)
            pc.playerCamera = cam;
        if (pc != null && gameMgr != null && pc.gameManager == null)
            pc.gameManager = gameMgr;
        if (pc != null && dsc != null && pc.depthSlider == null)
            pc.depthSlider = dsc;
        if (dsc != null && gm != null && dsc.gridManager == null)
            dsc.gridManager = gm;
        if (gameMgr != null && pc != null && gameMgr.playerController == null)
            gameMgr.playerController = pc;
        if (co != null && gm != null && co.target == null)
            co.target = gm.transform;
        if (cam != null && co == null)
        {
            co = cam.gameObject.AddComponent<CameraOrbit>();
            co.target = gm?.transform;
        }
    }
}
