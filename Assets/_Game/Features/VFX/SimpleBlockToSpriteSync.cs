using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Light_and_controller.Scripts.Components;
using UnityEngine;
using AmazingAssets.AdvancedDissolve;

/// <summary>
/// SPHERE-BASED DISSOLVE: Uses Advanced Dissolve Geometric Cutout (Sphere).
/// Auto-discovers active light sources from child LightDetectors at runtime.
/// Each Trigger's targetLightPoint becomes a sphere cutout position,
/// and its collider bounds determine the sphere radius.
/// Supports up to 4 simultaneous light sources per wall.
/// </summary>
public class SimpleBlockToSpriteSync : MonoBehaviour
{
    private const int MaxSpheres = 4;

    [Header("References")]
    [SerializeField] private SpriteRenderer wallSprite;

    [Header("Dissolve Settings")]
    [SerializeField] private bool invert = true;
    [SerializeField] private float noise = 0f;
    [SerializeField] private float defaultRadius = 2f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease animationEase = Ease.OutQuad;

    private Material _spriteMaterial;
    private AdvancedDissolveGeometricCutoutController _cutoutController;
    private LightDetector[] _detectors;

    // Track which Trigger GameObjects are currently assigned to which sphere slot
    private readonly Dictionary<GameObject, int> _activeLightSlots = new Dictionary<GameObject, int>();
    private readonly bool[] _slotOccupied = new bool[MaxSpheres];
    private readonly Tweener[] _slotTweens = new Tweener[MaxSpheres];
    private readonly float[] _slotRadius = new float[MaxSpheres];

    private void Start()
    {
        if (wallSprite == null)
        {
            Debug.LogWarning("Wall sprite not assigned on " + gameObject.name);
            return;
        }

        _spriteMaterial = wallSprite.material;

        // Find all LightDetectors in children (same as old grid approach found ObjectHiders)
        _detectors = GetComponentsInChildren<LightDetector>();
        if (_detectors.Length == 0)
        {
            Debug.LogWarning("No LightDetectors found in children of " + gameObject.name);
            return;
        }

        InitializeGeometricCutout();
    }

    private void InitializeGeometricCutout()
    {
        // Enable Advanced Dissolve State
        AdvancedDissolveKeywords.GetKeyword(_spriteMaterial, out AdvancedDissolveKeywords.State materialState);
        if (materialState != AdvancedDissolveKeywords.State.Enabled)
        {
            AdvancedDissolveKeywords.SetKeyword(_spriteMaterial, AdvancedDissolveKeywords.State.Enabled, true);
        }

        // Set Geometric Cutout type to Sphere
        AdvancedDissolveKeywords.SetKeyword(_spriteMaterial, AdvancedDissolveKeywords.CutoutGeometricType.Sphere, true);

        // Start with count of One (will update dynamically)
        AdvancedDissolveKeywords.SetKeyword(_spriteMaterial, AdvancedDissolveKeywords.CutoutGeometricCount.One, true);

        // Add or get the geometric cutout controller
        _cutoutController = GetComponent<AdvancedDissolveGeometricCutoutController>();
        if (_cutoutController == null)
        {
            _cutoutController = gameObject.AddComponent<AdvancedDissolveGeometricCutoutController>();
        }

        // Configure the controller
        _cutoutController.type = AdvancedDissolveKeywords.CutoutGeometricType.Sphere;
        _cutoutController.count = AdvancedDissolveKeywords.CutoutGeometricCount.One;
        _cutoutController.invert = invert;
        _cutoutController.noise = noise;
        _cutoutController.updateMode = AdvancedDissolveGeometricCutoutController.UpdateMode.EveryFrame;
        _cutoutController.materials = new List<Material> { _spriteMaterial };

        // Initialize all sphere slots with zero radius (invisible)
        for (int i = 0; i < MaxSpheres; i++)
        {
            var countID = (AdvancedDissolveKeywords.CutoutGeometricCount)i;
            _cutoutController.SetTargetStartPointPosition(countID, Vector3.zero);
            _cutoutController.SetTargetRadius(countID, 0f);
        }

        // Enable edge effects based on geometric cutout
        AdvancedDissolveKeywords.GetKeyword(_spriteMaterial, out AdvancedDissolveKeywords.EdgeBaseSource edgeSource);
        if (edgeSource == AdvancedDissolveKeywords.EdgeBaseSource.None)
        {
            AdvancedDissolveKeywords.SetKeyword(_spriteMaterial, AdvancedDissolveKeywords.EdgeBaseSource.CutoutGeometric, true);
        }

        _cutoutController.ForceUpdateShaderData();
    }

    private void Update()
    {
        if (_detectors == null || _cutoutController == null) return;

        // Collect all active light source GameObjects from all detectors
        HashSet<GameObject> currentLights = new HashSet<GameObject>();
        foreach (var detector in _detectors)
        {
            foreach (var lightGO in detector.lightSprings)
            {
                if (lightGO != null)
                    currentLights.Add(lightGO);
            }
        }

        // Remove lights that are no longer active — animate radius to 0
        var toRemove = _activeLightSlots.Keys.Where(go => !currentLights.Contains(go)).ToList();
        foreach (var go in toRemove)
        {
            int slot = _activeLightSlots[go];
            _activeLightSlots.Remove(go);
            AnimateSlotRadius(slot, 0f, () =>
            {
                var countID = (AdvancedDissolveKeywords.CutoutGeometricCount)slot;
                _cutoutController.SetTargetStartPointTransform(countID, null);
                _cutoutController.SetTargetStartPointPosition(countID, Vector3.zero);
                _slotOccupied[slot] = false;
            });
        }

        // Add new lights — animate radius from 0 to target
        foreach (var lightGO in currentLights)
        {
            if (_activeLightSlots.ContainsKey(lightGO)) continue;

            int freeSlot = -1;
            for (int i = 0; i < MaxSpheres; i++)
            {
                if (!_slotOccupied[i]) { freeSlot = i; break; }
            }

            if (freeSlot == -1) continue;

            var trigger = lightGO.GetComponent<Trigger>();
            if (trigger == null) continue;

            var countID = (AdvancedDissolveKeywords.CutoutGeometricCount)freeSlot;

            if (trigger.TargetLightPoint != null)
            {
                _cutoutController.SetTargetStartPointTransform(countID, trigger.TargetLightPoint);
            }

            float targetRadius = defaultRadius;
            if (trigger.LightCollider != null)
            {
                targetRadius = trigger.LightCollider.bounds.extents.magnitude;
            }

            _slotOccupied[freeSlot] = true;
            _activeLightSlots[lightGO] = freeSlot;
            _slotRadius[freeSlot] = 0f;
            _cutoutController.SetTargetRadius(countID, 0f);

            AnimateSlotRadius(freeSlot, targetRadius);
        }

        // Update sphere count keyword to match occupied slots
        int occupiedCount = _slotOccupied.Count(s => s);
        if (occupiedCount > 0)
        {
            var geometricCount = (AdvancedDissolveKeywords.CutoutGeometricCount)(Mathf.Min(occupiedCount, MaxSpheres) - 1);
            if (_cutoutController.count != geometricCount)
            {
                _cutoutController.count = geometricCount;
                AdvancedDissolveKeywords.SetKeyword(_spriteMaterial, geometricCount, true);
            }
        }
    }

    private void AnimateSlotRadius(int slot, float targetRadius, TweenCallback onComplete = null)
    {
        _slotTweens[slot]?.Kill();

        var countID = (AdvancedDissolveKeywords.CutoutGeometricCount)slot;
        _slotTweens[slot] = DOTween.To(
                () => _slotRadius[slot],
                value =>
                {
                    _slotRadius[slot] = value;
                    _cutoutController.SetTargetRadius(countID, value);
                },
                targetRadius,
                animationDuration
            )
            .SetEase(animationEase)
            .OnComplete(onComplete);
    }

    private void OnDestroy()
    {
        // Kill all active tweens
        for (int i = 0; i < MaxSpheres; i++)
        {
            _slotTweens[i]?.Kill();
        }

        // Only destroy if we created an instance material
        if (_spriteMaterial != null && wallSprite != null && _spriteMaterial != wallSprite.sharedMaterial)
        {
            Destroy(_spriteMaterial);
        }
    }
}
