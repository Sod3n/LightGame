using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper MonoBehaviour that provides a SpriteRenderer visible only in edit mode.
/// Useful for level design and visual reference during development.
/// </summary>
[ExecuteInEditMode]
[DisallowMultipleComponent]
public class EditModeSpriteRenderer : MonoBehaviour
{
    [Header("Sprite Settings")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color color = Color.white;
    [SerializeField] private int sortingOrder = 0;
    [SerializeField] private string sortingLayerName = "Default";

    [Header("Transform Settings")]
    [SerializeField] private Vector2 size = Vector2.one;
    [SerializeField] private bool lockToParentBounds = false;

    private SpriteRenderer spriteRenderer;
    private Transform cachedTransform;
    private Transform parentTransform;

#if UNITY_EDITOR
    private SpriteRenderer editorSpriteRenderer;
#endif

    private void Reset()
    {
        SetupSpriteRenderer();
    }

    private void OnValidate()
    {
        SetupSpriteRenderer();
        UpdateSpriteRendererProperties();
    }

    private void Awake()
    {
        SetupSpriteRenderer();
        cachedTransform = transform;
        parentTransform = transform.parent;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            CreateEditorSpriteRenderer();
            UpdateSpriteRendererProperties();
            EditorApplication.update += EditorUpdate;
        }
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            RemoveEditorSpriteRenderer();
            EditorApplication.update -= EditorUpdate;
        }
#endif
    }

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        if (!Application.isPlaying && lockToParentBounds && parentTransform != null)
        {
            UpdatePositionToParent();
        }
    }
#endif

    private void SetupSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
    }

#if UNITY_EDITOR
    private void CreateEditorSpriteRenderer()
    {
        if (editorSpriteRenderer == null)
        {
            // Check if editor sprites are globally enabled
            if (!EditorSpriteToggle.ShouldRenderEditorSprites()) return;

            var editorObj = new GameObject($"{name}_EditorSprite");
            editorObj.transform.SetParent(transform);
            editorObj.transform.localPosition = Vector3.zero;
            editorObj.transform.localRotation = Quaternion.identity;
            editorObj.transform.localScale = Vector3.one;

            editorSpriteRenderer = editorObj.AddComponent<SpriteRenderer>();
            editorSpriteRenderer.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
        }
    }

    private void RemoveEditorSpriteRenderer()
    {
        if (editorSpriteRenderer != null)
        {
            if (Application.isPlaying) return;

            if (editorSpriteRenderer.gameObject != null)
            {
                DestroyImmediate(editorSpriteRenderer.gameObject);
            }
            editorSpriteRenderer = null;
        }
    }
#endif

    private void UpdateSpriteRendererProperties()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = size;

#if UNITY_EDITOR
        // Check if editor sprites are globally disabled
        if (!EditorSpriteToggle.ShouldRenderEditorSprites())
        {
            RemoveEditorSpriteRenderer();
            return;
        }

        // If disabled but should be enabled, create it
        if (editorSpriteRenderer == null)
        {
            CreateEditorSpriteRenderer();
        }

        if (editorSpriteRenderer != null)
        {
            editorSpriteRenderer.sprite = sprite;
            editorSpriteRenderer.color = new Color(color.r, color.g, color.b, 0.5f); // Semi-transparent in editor
            editorSpriteRenderer.sortingOrder = sortingOrder;
            editorSpriteRenderer.sortingLayerName = sortingLayerName;
            editorSpriteRenderer.drawMode = SpriteDrawMode.Sliced;
            editorSpriteRenderer.size = size;

            // Use unlit material for editor sprite to avoid lighting interference
            SetupUnlitMaterial(editorSpriteRenderer);
        }
#endif
    }

#if UNITY_EDITOR
    private void SetupUnlitMaterial(SpriteRenderer renderer)
    {
        if (renderer == null) return;

        // Use unlit material instead of default lit material
        if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null ||
            renderer.sharedMaterial.shader.name.Contains("Lit"))
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
                renderer.sharedMaterial = unlitMaterial;
            }
        }
    }
#endif

#if UNITY_EDITOR
    private void UpdatePositionToParent()
    {
        if (editorSpriteRenderer != null && parentTransform != null)
        {
            editorSpriteRenderer.transform.position = parentTransform.position;
            editorSpriteRenderer.transform.rotation = parentTransform.rotation;
            editorSpriteRenderer.transform.localScale = parentTransform.localScale;
        }
    }
#endif

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && sprite != null && EditorSpriteToggle.ShouldRenderEditorSprites())
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
#endif

    /// <summary>
    /// Updates the sprite at runtime (useful for testing in play mode)
    /// </summary>
    public void SetSprite(Sprite newSprite)
    {
        sprite = newSprite;
        UpdateSpriteRendererProperties();
    }

    /// <summary>
    /// Updates the color at runtime
    /// </summary>
    public void SetColor(Color newColor)
    {
        color = newColor;
        UpdateSpriteRendererProperties();
    }

    /// <summary>
    /// Updates the size at runtime
    /// </summary>
    public void SetSize(Vector2 newSize)
    {
        size = newSize;
        UpdateSpriteRendererProperties();
    }

#if UNITY_EDITOR
    [ContextMenu("Snap to Parent Bounds")]
    private void SnapToParentBounds()
    {
        if (parentTransform != null)
        {
            transform.position = parentTransform.position;
            transform.rotation = parentTransform.rotation;
            transform.localScale = parentTransform.localScale;
        }
    }

    [ContextMenu("Create Editor Sprite")]
    private void CreateEditorSpriteContext()
    {
        CreateEditorSpriteRenderer();
        UpdateSpriteRendererProperties();
    }

    [ContextMenu("Remove Editor Sprite")]
    private void RemoveEditorSpriteContext()
    {
        RemoveEditorSpriteRenderer();
    }
#endif
}