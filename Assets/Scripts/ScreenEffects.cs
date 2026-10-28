using UnityEngine;
using System.Collections;

public class ScreenEffects : MonoBehaviour
{
    [Header("Glitch Effect")]
    public Material glitchMaterial;
    
    [Header("Screen Shake")]
    public float shakeAmount = 0.1f;
    public float shakeDuration = 0.5f;
    
    private Vector3 originalPosition;
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera != null)
            originalPosition = mainCamera.transform.localPosition;
    }
    
    public void TriggerShake(float amount = 0.1f, float duration = 0.5f)
    {
        StartCoroutine(ScreenShake(amount, duration));
    }
    
    private IEnumerator ScreenShake(float amount, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * amount;
            float y = Random.Range(-1f, 1f) * amount;
            if (mainCamera != null)
                mainCamera.transform.localPosition = originalPosition + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (mainCamera != null)
            mainCamera.transform.localPosition = originalPosition;
    }
    
    public void TriggerGlitch(float duration = 1f)
    {
        StartCoroutine(GlitchEffect(duration));
    }
    
    private IEnumerator GlitchEffect(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Simple glitch - could be expanded with shader
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
