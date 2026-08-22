using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LightGame.Features
{
    /// <summary>
    /// Consolidates the outlines of many static child colliders into a single, separate shadow-only
    /// <see cref="PolygonCollider2D"/> (one path per source shape) that feeds a
    /// <see cref="CompositeCollider2D"/>. One <see cref="UnityEngine.Rendering.Universal.ShadowCaster2D"/>
    /// then casts the whole level's shadow from that one merged collider.
    ///
    /// Source colliders are only <b>read</b>, never modified or consumed, so all gameplay that relies on
    /// them (triggers, raycasts, OnCollision, OverlapPoint, ...) keeps working. The only thing this
    /// component adds is the one PolygonCollider2D on its own GameObject.
    ///
    /// IMPORTANT: the composite is a real static collider. Put this GameObject on a physics layer that
    /// collides with nothing (Project Settings > Physics 2D > Layer Collision Matrix) so it never
    /// affects gameplay collision. The shadow shape provider reads the geometry directly and ignores the
    /// collision matrix, so shadows still work.
    ///
    /// Point your ShadowCaster2D's Casting Source at the <b>CompositeCollider2D</b> on this object, and
    /// set its Target Sorting Layers to exclude the ground layer.
    ///
    /// Prefer baking in the editor (context menu) so the result serializes into the scene with no
    /// load-time hitch. Enable <see cref="bakeAtRuntime"/> only for procedural levels.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public class ShadowOccluderBaker : MonoBehaviour
    {
        [Header("Scan")]
        [Tooltip("Groups to scan for occluder colliders. Defaults to this object's own children when empty.")]
        [SerializeField] private Transform[] rootsToScan;

        [Tooltip("Only colliders on these layers are consolidated into the shadow shape.")]
        [SerializeField] private LayerMask occluderLayers = ~0;

        [Tooltip("Include trigger colliders. Off by default (triggers are usually gameplay zones, not occluders).")]
        [SerializeField] private bool includeTriggers;

        [Header("Curve tessellation")]
        [Tooltip("Segments used to approximate a CircleCollider2D outline.")]
        [Range(6, 48)] [SerializeField] private int circleSegments = 20;

        [Tooltip("Segments per rounded cap used to approximate a CapsuleCollider2D outline.")]
        [Range(2, 24)] [SerializeField] private int capsuleCapSegments = 8;

        [Header("Runtime")]
        [Tooltip("Bake at Awake instead of only in the editor. Costs a one-time load hitch; " +
                 "use only when the level geometry is generated at runtime.")]
        [SerializeField] private bool bakeAtRuntime;

        private void Awake()
        {
            if (bakeAtRuntime)
                Bake();
        }

        /// <summary>
        /// Reads every eligible static child collider, converts its outline into this object's local
        /// space, and rebuilds the single shadow PolygonCollider2D + merged composite. Never modifies
        /// the source colliders. Returns the number of paths generated.
        /// </summary>
        public int Bake()
        {
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            // Baking happens in this object's local space, so a non-identity transform collapses or
            // distorts the generated vertices. Warn instead of silently producing degenerate shapes.
            var scale = transform.lossyScale;
            if (Mathf.Abs(scale.x) < 0.99f || Mathf.Abs(scale.y) < 0.99f ||
                Mathf.Abs(scale.x - scale.y) > 0.01f)
            {
                Debug.LogWarning($"[ShadowOccluderBaker] This object's world scale is {scale}. Keep it at " +
                                 "(1,1,1) with no rotation, or baked outlines may fail verification (too small).", this);
            }

            if (!TryGetComponent<CompositeCollider2D>(out var composite))
                composite = gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            var poly = GetComponent<PolygonCollider2D>();
            poly.isTrigger = false;
            poly.offset = Vector2.zero;
            poly.compositeOperation = Collider2D.CompositeOperation.Merge;

            var roots = new List<Transform>();
            if (rootsToScan != null && rootsToScan.Length > 0)
            {
                foreach (var root in rootsToScan)
                    if (root != null)
                        roots.Add(root);
            }
            if (roots.Count == 0)
                roots.Add(transform);

            var toLocal = transform.worldToLocalMatrix;
            var paths = new List<Vector2[]>();
            var seen = new HashSet<Collider2D>();
            int descendantsSkipped = 0;

            foreach (var root in roots)
            {
                foreach (var col in root.GetComponentsInChildren<Collider2D>(true))
                {
                    if (col is CompositeCollider2D) continue;
                    if (col.transform == transform) continue;                          // our own shadow collider
                    // Occluders must NOT be descendants of this baker, or their bodies get pulled into the
                    // composite and their individual collision breaks. Read them from a separate object.
                    if (col.transform.IsChildOf(transform)) { descendantsSkipped++; continue; }
                    if (col.isTrigger && !includeTriggers) continue;
                    if (((1 << col.gameObject.layer) & occluderLayers.value) == 0) continue;
                    if (!seen.Add(col)) continue;

                    var m = toLocal * col.transform.localToWorldMatrix;
                    ExtractPaths(col, m, paths);
                }
            }

            if (descendantsSkipped > 0)
            {
                Debug.LogError($"[ShadowOccluderBaker] {descendantsSkipped} collider(s) are children of this baker " +
                               "and were skipped. Put the baker on a SEPARATE object (not a parent of the occluders) " +
                               "and point 'rootsToScan' at the occluder group, so gameplay colliders are read, not consumed.", this);
            }

            poly.pathCount = paths.Count;
            for (int i = 0; i < paths.Count; i++)
                poly.SetPath(i, paths[i]);

            composite.GenerateGeometry();

            Debug.Log($"[ShadowOccluderBaker] Consolidated {paths.Count} collider outline(s) into one shadow collider.", this);
            return paths.Count;
        }

        // Appends one closed outline (in target-local space) per source shape to 'paths'.
        private void ExtractPaths(Collider2D col, Matrix4x4 m, List<Vector2[]> paths)
        {
            switch (col)
            {
                case BoxCollider2D box:
                {
                    Vector2 h = box.size * 0.5f;
                    Vector2 o = box.offset;
                    paths.Add(Transform(m, new[]
                    {
                        o + new Vector2(-h.x, -h.y),
                        o + new Vector2(-h.x,  h.y),
                        o + new Vector2( h.x,  h.y),
                        o + new Vector2( h.x, -h.y),
                    }));
                    break;
                }
                case PolygonCollider2D poly:
                {
                    for (int i = 0; i < poly.pathCount; i++)
                    {
                        var pts = poly.GetPath(i);
                        for (int k = 0; k < pts.Length; k++)
                            pts[k] += poly.offset;
                        paths.Add(Transform(m, pts));
                    }
                    break;
                }
                case CircleCollider2D circle:
                {
                    paths.Add(Transform(m, BuildArc(circle.offset, circle.radius, 0f, 360f, circleSegments)));
                    break;
                }
                case CapsuleCollider2D capsule:
                {
                    paths.Add(Transform(m, BuildCapsule(capsule)));
                    break;
                }
                default:
                    Debug.LogWarning($"[ShadowOccluderBaker] Unsupported collider '{col.GetType().Name}' " +
                                     $"on '{col.name}'; skipped.", col);
                    break;
            }
        }

        private static Vector2[] Transform(Matrix4x4 m, Vector2[] local)
        {
            var outPts = new Vector2[local.Length];
            for (int i = 0; i < local.Length; i++)
                outPts[i] = m.MultiplyPoint3x4(local[i]);
            return outPts;
        }

        private static Vector2[] BuildArc(Vector2 center, float radius, float startDeg, float endDeg, int segments)
        {
            var pts = new Vector2[segments];
            float step = (endDeg - startDeg) / segments;
            for (int i = 0; i < segments; i++)
            {
                float a = (startDeg + step * i) * Mathf.Deg2Rad;
                pts[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            return pts;
        }

        private Vector2[] BuildCapsule(CapsuleCollider2D capsule)
        {
            Vector2 size = capsule.size;
            Vector2 o = capsule.offset;
            bool vertical = capsule.direction == CapsuleDirection2D.Vertical;

            float radius = vertical ? size.x * 0.5f : size.y * 0.5f;
            float half = vertical ? Mathf.Max(0f, size.y * 0.5f - radius)
                                  : Mathf.Max(0f, size.x * 0.5f - radius);

            var pts = new List<Vector2>((capsuleCapSegments + 1) * 2);
            int seg = capsuleCapSegments;

            if (vertical)
            {
                var top = o + new Vector2(0f, half);
                var bot = o + new Vector2(0f, -half);
                for (int i = 0; i <= seg; i++)   // top cap: right -> left
                {
                    float a = Mathf.Lerp(0f, 180f, (float)i / seg) * Mathf.Deg2Rad;
                    pts.Add(top + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
                for (int i = 0; i <= seg; i++)   // bottom cap: left -> right
                {
                    float a = Mathf.Lerp(180f, 360f, (float)i / seg) * Mathf.Deg2Rad;
                    pts.Add(bot + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
            }
            else
            {
                var right = o + new Vector2(half, 0f);
                var left = o + new Vector2(-half, 0f);
                for (int i = 0; i <= seg; i++)   // right cap: bottom -> top
                {
                    float a = Mathf.Lerp(-90f, 90f, (float)i / seg) * Mathf.Deg2Rad;
                    pts.Add(right + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
                for (int i = 0; i <= seg; i++)   // left cap: top -> bottom
                {
                    float a = Mathf.Lerp(90f, 270f, (float)i / seg) * Mathf.Deg2Rad;
                    pts.Add(left + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
            }
            return pts.ToArray();
        }

#if UNITY_EDITOR
        [ContextMenu("Bake Shadow Occluders")]
        private void BakeFromMenu()
        {
            Bake();
            EditorUtility.SetDirty(this);
            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}
