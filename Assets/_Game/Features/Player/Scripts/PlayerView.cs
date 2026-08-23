using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMain playerMain;
    
    [Header("Landing Effect Settings")]
    [SerializeField] private GameObject landingEffectPrefab;
    [SerializeField] private Transform landingSpawnPoint;
    
    [Header("Fall Time Scaling")]
    [SerializeField] private bool scaleEffectByFallTime = true;
    [SerializeField] private float minFallTimeForEffect = 0.1f;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 2f;
    [SerializeField] private float fallTimeScaleMultiplier = 1f;

    private PlayerLandState LandState => playerMain?.LandState as PlayerLandState;
    
    private void Awake()
    {
        if (playerMain == null)
            playerMain = GetComponent<PlayerMain>();
    }

    private void Start()
    {
        if (playerMain != null && LandState != null)
        {
            LandState.OnLanded += OnPlayerLanded;
        }
    }

    private void OnDisable()
    {
        if (playerMain != null && LandState != null)
        {
            LandState.OnLanded -= OnPlayerLanded;
        }
    }

    private void OnPlayerLanded(float fallTime)
    {
        if (landingEffectPrefab == null)
            return;

        if (fallTime < minFallTimeForEffect)
            return;

        Vector3 spawnPosition = GetLandingSpawnPosition();
        GameObject effect = Instantiate(landingEffectPrefab, spawnPosition, Quaternion.identity);

        if (scaleEffectByFallTime)
        {
            float scale = CalculateScaleFromFallTime(fallTime);
            effect.transform.localScale = Vector3.one * scale;
        }
    }

    private Vector3 GetLandingSpawnPosition()
    {
        if (landingSpawnPoint != null)
        {
            return landingSpawnPoint.position;
        }

        return transform.position;
    }

    private float CalculateScaleFromFallTime(float fallTime)
    {
        float scaledTime = fallTime * fallTimeScaleMultiplier;
        return Mathf.Clamp(scaledTime, minScale, maxScale);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 spawnPos = GetLandingSpawnPosition();
        Gizmos.DrawWireSphere(spawnPos, 0.2f);
        Gizmos.DrawLine(transform.position, spawnPos);
    }
#endif
}
