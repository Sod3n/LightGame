using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using LightGame.Features;
using UnityEngine;

/// <summary>
/// SPHERE-BASED DISSOLVE for the 2D-lit dissolve shader (LightGame/2D/SpriteLitDissolve).
/// Auto-discovers active light sources from child LightDetectors at runtime. Each active light's
/// targetLightPoint becomes a world-space sphere cutout position, its collider bounds set the
/// radius, and the radius animates in/out. Supports up to 4 simultaneous light sources per wall.
///
/// Replaces the old Amazing Assets Advanced Dissolve controller: the shader receives Light2D and
/// 2D shadows, so the platform is lit correctly while it dissolves.
/// </summary>
public class SimpleBlockToSpriteSync : MonoBehaviour
{
    private const int MaxSpheres = 4;
    private const float FarAway = 1e6f; // parks unused sphere slots so they never cut

    [Header("References")]
    [SerializeField] private SpriteRenderer wallSprite;

    [Header("Dissolve Settings")]
    [Tooltip("Dissolve INSIDE the light spheres (matches the old 'invert' behaviour).")]
    [SerializeField] private bool invert = true;
    [Tooltip("Edge noise amount fed to the shader's _DissolveNoise.")]
    [SerializeField] private float noise = 0f;
    [SerializeField] private float defaultRadius = 2f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease animationEase = Ease.OutQuad;

    private static readonly int CutoutSpheresID = Shader.PropertyToID("_CutoutSpheres");
    private static readonly int DissolveInvertID = Shader.PropertyToID("_DissolveInvert");
    private static readonly int DissolveNoiseID = Shader.PropertyToID("_DissolveNoise");

    private Material _spriteMaterial;
    private LightDetector[] _detectors;

    private readonly Dictionary<GameObject, int> _activeLightSlots = new Dictionary<GameObject, int>();
    private readonly bool[] _slotOccupied = new bool[MaxSpheres];
    private readonly Tweener[] _slotTweens = new Tweener[MaxSpheres];
    private readonly float[] _slotRadius = new float[MaxSpheres];
    private readonly Transform[] _slotPoint = new Transform[MaxSpheres];
    private readonly Vector4[] _sphereData = new Vector4[MaxSpheres];

    private void Start()
    {
        if (wallSprite == null)
        {
            Debug.LogWarning("Wall sprite not assigned on " + gameObject.name);
            return;
        }

        _spriteMaterial = wallSprite.material;

        _detectors = GetComponentsInChildren<LightDetector>();
        if (_detectors.Length == 0)
        {
            Debug.LogWarning("No LightDetectors found in children of " + gameObject.name);
            return;
        }

        _spriteMaterial.SetFloat(DissolveInvertID, invert ? 1f : 0f);
        _spriteMaterial.SetFloat(DissolveNoiseID, noise);

        // Park every slot far away with zero radius so nothing is cut initially.
        for (int i = 0; i < MaxSpheres; i++)
            _sphereData[i] = new Vector4(FarAway, FarAway, 0f, 0f);
        _spriteMaterial.SetVectorArray(CutoutSpheresID, _sphereData);
    }

    private void Update()
    {
        if (_detectors == null || _spriteMaterial == null) return;

        // Collect all active light source GameObjects from all detectors.
        HashSet<GameObject> currentLights = new HashSet<GameObject>();
        foreach (var detector in _detectors)
            foreach (var lightGO in detector.lightSprings)
                if (lightGO != null)
                    currentLights.Add(lightGO);

        // Remove lights that are no longer active — animate radius to 0, then free the slot.
        var toRemove = _activeLightSlots.Keys.Where(go => !currentLights.Contains(go)).ToList();
        foreach (var go in toRemove)
        {
            int slot = _activeLightSlots[go];
            _activeLightSlots.Remove(go);
            AnimateSlotRadius(slot, 0f, () =>
            {
                _slotOccupied[slot] = false;
                _slotPoint[slot] = null;
            });
        }

        // Add new lights — animate radius from 0 to target.
        foreach (var lightGO in currentLights)
        {
            if (_activeLightSlots.ContainsKey(lightGO)) continue;

            int freeSlot = -1;
            for (int i = 0; i < MaxSpheres; i++)
                if (!_slotOccupied[i]) { freeSlot = i; break; }
            if (freeSlot == -1) continue;

            var trigger = lightGO.GetComponent<LightSource>();
            if (trigger == null) continue;

            _slotPoint[freeSlot] = trigger.TargetLightPoint != null ? trigger.TargetLightPoint : lightGO.transform;

            float targetRadius = trigger.LightCollider != null
                ? trigger.LightCollider.bounds.extents.magnitude
                : defaultRadius;

            _slotOccupied[freeSlot] = true;
            _activeLightSlots[lightGO] = freeSlot;
            _slotRadius[freeSlot] = 0f;
            AnimateSlotRadius(freeSlot, targetRadius);
        }

        // Push current sphere positions (world) + radii to the shader.
        for (int i = 0; i < MaxSpheres; i++)
        {
            if (_slotOccupied[i] && _slotPoint[i] != null)
            {
                Vector3 p = _slotPoint[i].position;
                _sphereData[i] = new Vector4(p.x, p.y, 0f, _slotRadius[i]);
            }
            else
            {
                _sphereData[i] = new Vector4(FarAway, FarAway, 0f, 0f);
            }
        }
        _spriteMaterial.SetVectorArray(CutoutSpheresID, _sphereData);
    }

    private void AnimateSlotRadius(int slot, float targetRadius, TweenCallback onComplete = null)
    {
        _slotTweens[slot]?.Kill();
        _slotTweens[slot] = DOTween.To(
                () => _slotRadius[slot],
                value => _slotRadius[slot] = value,
                targetRadius,
                animationDuration)
            .SetEase(animationEase)
            .OnComplete(onComplete);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < MaxSpheres; i++)
            _slotTweens[i]?.Kill();

        if (_spriteMaterial != null && wallSprite != null && _spriteMaterial != wallSprite.sharedMaterial)
            Destroy(_spriteMaterial);
    }
}
