using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LightGame.Features;
using LightGame;

using LightGame.Globals;
public class LightSource : MonoBehaviour
{
    [Header("Wiring (auto-filled from this GameObject in OnValidate)")]
    [SerializeField, Tooltip("Point in space the light originates from. Used by LightDetector to check whether the light is blocked by geometry.")]
    private Transform targetLightPoint;
    [SerializeField, Tooltip("The trigger collider on this GameObject. Auto-wired if left empty.")]
    private Collider2D collider2D;

    [Header("Light Behavior")]
    [SerializeField, Tooltip("Default: normal light. LevelChange: triggers a scene transition when the player stands in the light.")]
    private LightType lightType = LightType.Default;

    [Header("Level Change Settings (only used when Light Type = LevelChange)")]
    [SerializeField, Tooltip("If ON: load the next scene in Game.LevelOrder. If OFF: load the specific Target Scene below.")]
    private bool useNextScene = true;
    [SerializeField, Tooltip("Scene to load when the player enters the light. Ignored if Use Next Scene is ON.")]
    private SceneName targetScene;

    [Header("Teleport Landing (every light is a teleport target)")]
    [SerializeField, Tooltip("Optional: specific Transform where the player lands when teleporting here. If null, uses this GameObject's position.")]
    private Transform teleportLandingPoint;
    [SerializeField, Tooltip("Extra offset applied to the landing point.")]
    private Vector2 teleportLandingOffset = Vector2.zero;

    public LightType LightType => lightType;
    public bool UseNextScene => useNextScene;
    public SceneName TargetScene => targetScene;
    public Transform TargetLightPoint => targetLightPoint;
    public Collider2D LightCollider => collider2D;

    private readonly HashSet<LightDetector> _registeredDetectors = new HashSet<LightDetector>();

    public Vector2 GetTeleportLandingPosition()
    {
        Vector2 basePos = teleportLandingPoint != null
            ? (Vector2)teleportLandingPoint.position
            : (Vector2)transform.position;
        return basePos + teleportLandingOffset;
    }

    private void Reset()
    {
        gameObject.tag = "Teleport";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector2 dest = GetTeleportLandingPosition();
        Gizmos.DrawWireSphere(dest, 0.35f);
        Gizmos.DrawLine(transform.position, dest);
    }

    private void OnValidate()
    {
        if (collider2D == null) collider2D = GetComponent<Collider2D>();
        if (targetLightPoint == null) targetLightPoint = transform;
    }

    private async void OnEnable()
    {
        // Re-scan on every enable so toggled lights pick up already-overlapping detectors.
        // OnTriggerEnter doesn't reliably fire when a collider is re-enabled while overlapping,
        // and GetContacts only returns pairs where at least one side has a Rigidbody2D — static
        // detectors (HidePlatform etc.) never show up. Overlap() works for static colliders too.
        await UniTask.WaitForFixedUpdate();
        if (this == null || !isActiveAndEnabled || collider2D == null) return;

        var filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.NoFilter();
        var overlaps = new List<Collider2D>();
        collider2D.Overlap(filter, overlaps);
        foreach (var contact in overlaps)
        {
            if (contact != null) Handle(contact);
        }
    }

    private void OnDisable()
    {
        // Disabled MonoBehaviours don't get OnTriggerExit — clear registrations manually.
        foreach (var detector in _registeredDetectors)
        {
            if (detector != null) detector.RemoveLightSource(gameObject, lightType);
        }
        _registeredDetectors.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Handle(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Handle(other); 
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if(!other.TryGetComponent<LightDetector>(out var component)) return;
        var contacts = new List<Collider2D>();
        collider2D.GetContacts(contacts);
        if(contacts.Any(x => x.gameObject == other.gameObject)) return;

        component.RemoveLightSource(gameObject, lightType);
        _registeredDetectors.Remove(component);
    }

    private void Handle(Collider2D other)
    {
        if(!other.TryGetComponent<LightDetector>(out var component)) return;

        var is_lighted = component.LightBlockCheck(targetLightPoint.position);
        if (is_lighted)
        {
            component.AddLightSource(gameObject, lightType);
            _registeredDetectors.Add(component);
        }
        else
        {
            component.RemoveLightSource(gameObject, lightType);
            _registeredDetectors.Remove(component);
        }
    }
}
