using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Simple helper MonoBehaviour that provides an editor-only sprite visualization.
/// Perfect for level design and layout reference.
/// </summary>
[ExecuteInEditMode]
[DisallowMultipleComponent]
public class SimpleEditModeSprite : MonoBehaviour
{
    [Header("Editor-Only Sprite Settings")]
    [SerializeField] private Sprite editorSprite;
    [SerializeField] private Color editorColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Vector2 spriteSize = Vector2.one;
    [SerializeField] private int sortingOrder = 0;
    [SerializeField] private string sortingLayer = "Default";

#if UNITY_EDITOR
    private SpriteRenderer editorRenderer;
    private GameObject editorObject;
#endif

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            CreateEditorSprite();
        }
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyEditorSprite();
        }
#endif
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && editorRenderer != null)
        {
            UpdateEditorSprite();
        }
#endif
    }

#if UNITY_EDITOR
    private void CreateEditorSprite()
    {
        if (editorRenderer != null) return;

        // Check if editor sprites are globally enabled
        if (!EditorSpriteToggle.ShouldRenderEditorSprites()) return;

        editorObject = new GameObject("Editor_Only_Sprite");
        editorObject.transform.SetParent(transform);
        editorObject.transform.localPosition = Vector3.zero;
        editorObject.transform.localRotation = Quaternion.identity;
        editorObject.transform.localScale = Vector3.one;
        editorObject.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;

        editorRenderer = editorObject.AddComponent<SpriteRenderer>();
        editorRenderer.hideFlags = HideFlags.DontSave;
        UpdateEditorSprite();
    }

    private void DestroyEditorSprite()
    {
        if (editorObject != null)
        {
            DestroyImmediate(editorObject);
            editorObject = null;
            editorRenderer = null;
        }
    }

    private void UpdateEditorSprite()
    {
        // Check if editor sprites are globally disabled
        if (!EditorSpriteToggle.ShouldRenderEditorSprites())
        {
            DestroyEditorSprite();
            return;
        }

        // If disabled but should be enabled, create it
        if (editorRenderer == null)
        {
            CreateEditorSprite();
            return;
        }

        editorRenderer.sprite = editorSprite;
        editorRenderer.color = editorColor;
        editorRenderer.sortingOrder = sortingOrder;
        editorRenderer.sortingLayerName = sortingLayer;
        editorRenderer.drawMode = SpriteDrawMode.Sliced;
        editorRenderer.size = spriteSize;

        // Use unlit material instead of default lit material
        if (editorRenderer.sharedMaterial == null || editorRenderer.sharedMaterial.shader == null ||
            editorRenderer.sharedMaterial.shader.name.Contains("Lit"))
        {
            // Try URP 2D sprite unlit first (for URP projects)
            var urpUnlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (urpUnlitShader == null)
            {
                // Fallback to regular unlit shader
                urpUnlitShader = Shader.Find("Unlit/Texture");
                if (urpUnlitShader == null)
                {
                    // Final fallback to sprites default
                    urpUnlitShader = Shader.Find("Sprites/Default");
                }
            }

            if (urpUnlitShader != null)
            {
                var unlitMaterial = new Material(urpUnlitShader);
                unlitMaterial.hideFlags = HideFlags.DontSave;
                editorRenderer.sharedMaterial = unlitMaterial;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && editorSprite != null && EditorSpriteToggle.ShouldRenderEditorSprites())
        {
            Gizmos.color = new Color(editorColor.r, editorColor.g, editorColor.b, 0.3f);
            Gizmos.DrawWireCube(transform.position, spriteSize);
        }
    }
#endif
}