using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace LightGame.Features.VFX
{
    // Runs before SpriteAnimationCrossfade and SpriteTransitionSpring so they see the matched frame.
    [DefaultExecutionOrder(-50)]
    public class AnimatorPoseMatcher : MonoBehaviour
    {
        [Serializable]
        public class Match
        {
            public string state;
            public Sprite from;
            [Range(0, 1)] public float normalizedTime;
        }

        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private int layer;
        [Tooltip("Looping states that start on the frame closest to the outgoing pose instead of their first frame.")]
        [SerializeField] private List<string> matchedStates = new();
        [Tooltip("Frames whose sprite name ends with this are never picked as a start frame. Generated in-betweens are softer, so they would otherwise score as closest to everything.")]
        [SerializeField] private string skipSpriteSuffix = "_inb";
        [Tooltip("Baked from the controller's clips with the context menu; rebake after changing sprites.")]
        [SerializeField] private List<Match> matches = new();

        private readonly Dictionary<(int, Sprite), float> _lookup = new();
        private int _lastState;
        private Sprite _lastSprite;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            foreach (var m in matches)
                if (m.from != null) _lookup[(Animator.StringToHash(m.state), m.from)] = m.normalizedTime;
        }

        private void OnDisable() => _lastState = 0;

        private void LateUpdate()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;

            var state = animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
            if (_lastState != 0 && state != _lastState && _lastSprite != null &&
                _lookup.TryGetValue((state, _lastSprite), out var time))
            {
                animator.Play(state, layer, time);
                animator.Update(0);
            }
            _lastState = state;
            _lastSprite = spriteRenderer.sprite;
        }

#if UNITY_EDITOR
        private const int Resolution = 48;
        private const int Supersample = 3;

        [ContextMenu("Bake pose matches")]
        private void Bake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            var controller = (AnimatorController)animator.runtimeAnimatorController;
            var path = AnimationUtility.CalculateTransformPath(spriteRenderer.transform, animator.transform);

            var keysByState = new Dictionary<string, (AnimationClip clip, ObjectReferenceKeyframe[] keys)>();
            foreach (var cs in controller.layers[layer].stateMachine.states)
            {
                if (cs.state.motion is not AnimationClip clip) continue;
                var binding = EditorCurveBinding.PPtrCurve(path, typeof(SpriteRenderer), "m_Sprite");
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (keys is { Length: > 0 }) keysByState[cs.state.name] = (clip, keys);
            }

            var sprites = keysByState.Values.SelectMany(v => v.keys).Select(k => k.value as Sprite)
                .Where(s => s != null).Distinct().ToList();
            var box = sprites[0].bounds;
            foreach (var s in sprites) box.Encapsulate(s.bounds);

            var readable = new Dictionary<Texture2D, Texture2D>();
            var descriptors = sprites.ToDictionary(s => s, s => Describe(s, box, readable));
            foreach (var t in readable.Values) DestroyImmediate(t);

            Undo.RecordObject(this, "Bake pose matches");
            matches.Clear();
            foreach (var stateName in matchedStates)
            {
                if (!keysByState.TryGetValue(stateName, out var target)) continue;
                foreach (var s in sprites)
                {
                    var best = 0;
                    var bestDistance = float.MaxValue;
                    for (var k = 0; k < target.keys.Length; k++)
                    {
                        if (target.keys[k].value is not Sprite candidate) continue;
                        if (!string.IsNullOrEmpty(skipSpriteSuffix) && candidate.name.EndsWith(skipSpriteSuffix)) continue;
                        var d = Distance(descriptors[s], descriptors[candidate]);
                        if (d < bestDistance) { bestDistance = d; best = k; }
                    }
                    var time = Mathf.Repeat(target.keys[best].time / target.clip.length + 0.001f, 1);
                    matches.Add(new Match { state = stateName, from = s, normalizedTime = time });
                }
            }
            EditorUtility.SetDirty(this);
            Debug.Log($"{name}: baked {matches.Count} pose matches for {matchedStates.Count} states from {sprites.Count} sprites");
        }

        private static float[] Describe(Sprite sprite, Bounds box, Dictionary<Texture2D, Texture2D> readable)
        {
            var texture = sprite.texture;
            if (!readable.TryGetValue(texture, out var pixels))
            {
                var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(texture, rt);
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                pixels.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                readable[texture] = pixels;
            }

            var result = new float[Resolution * Resolution * 4];
            var cell = new Vector2(box.size.x / Resolution, box.size.y / Resolution);
            var rect = sprite.rect;
            var origin = rect.position + sprite.pivot;
            for (var y = 0; y < Resolution; y++)
            for (var x = 0; x < Resolution; x++)
            {
                var sum = Vector4.zero;
                for (var sy = 0; sy < Supersample; sy++)
                for (var sx = 0; sx < Supersample; sx++)
                {
                    var local = new Vector2(box.min.x + (x + (sx + 0.5f) / Supersample) * cell.x,
                                            box.min.y + (y + (sy + 0.5f) / Supersample) * cell.y);
                    var p = origin + local * sprite.pixelsPerUnit;
                    if (!rect.Contains(p)) continue;
                    var c = pixels.GetPixelBilinear(p.x / texture.width, p.y / texture.height);
                    sum += new Vector4(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                }
                sum /= Supersample * Supersample;
                var i = (y * Resolution + x) * 4;
                result[i] = sum.x; result[i + 1] = sum.y; result[i + 2] = sum.z; result[i + 3] = sum.w;
            }
            return result;
        }

        private static float Distance(float[] a, float[] b)
        {
            var d = 0f;
            for (var i = 0; i < a.Length; i++) d += (a[i] - b[i]) * (a[i] - b[i]);
            return d;
        }
#endif
    }
}
