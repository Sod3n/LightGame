using UnityEngine;

/// <summary>
/// Bends a SpriteRenderer's mesh to conform to the ground surface beneath it.
/// Casts rays from a single center point, fanning outward to sample terrain height,
/// then generates a deformed mesh that follows the ground contour.
/// Useful for effects (shadows, decals) that need to wrap curved terrain.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpriteBendToGround : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private SpriteRenderer sourceSprite;

    [Header("Raycast Settings")]
    [Tooltip("Number of sample points across the sprite width.")]
    [SerializeField, Range(2, 64)] private int segments = 16;
    [Tooltip("Layer mask for ground detection.")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [Tooltip("Height above the transform origin for the single raycast origin point.")]
    [SerializeField] private float raycastHeight = 5f;
    [Tooltip("Half-angle of the ray fan in degrees (e.g. 45 = 90° total spread).")]
    [SerializeField, Range(1f, 85f)] private float spreadAngle = 45f;
    [Tooltip("Maximum ray distance.")]
    [SerializeField] private float rayMaxDistance = 20f;
    [Tooltip("Small offset above the ground surface.")]
    [SerializeField] private float surfaceOffset = 0.01f;

    [Header("Shape")]
    [Tooltip("Override width. If 0, uses the source sprite's width.")]
    [SerializeField] private float widthOverride = 0f;
    [Tooltip("Multiplier applied to the auto-calculated height (1 = original sprite ratio).")]
    [SerializeField] private float heightScale = 1f;
    [Tooltip("Number of smoothing passes applied to ground heights. Higher = smoother transitions between segments.")]
    [SerializeField, Range(0, 10)] private int smoothingPasses = 2;

    [Header("Update")]
    [SerializeField] private bool updateEveryFrame = true;

    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Material _instanceMaterial;

    private Vector3[] _vertices;
    private Vector2[] _uvs;
    private int[] _triangles;
    private Color[] _colors;
    private float[] _groundHeights;
    private float[] _smoothBuffer;
    private float[] _hitXPositions;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh { name = "BendToGroundMesh" };
        _meshFilter.mesh = _mesh;

        SetupMaterial();
        AllocateBuffers();
        RebuildMesh();
    }

    private void LateUpdate()
    {
        if (updateEveryFrame)
        {
            RebuildMesh();
        }
    }

    /// <summary>
    /// Call this to manually refresh the mesh (useful when updateEveryFrame is false).
    /// </summary>
    public void Refresh()
    {
        RebuildMesh();
    }

    /// <summary>
    /// Assign a new source sprite at runtime.
    /// </summary>
    public void SetSourceSprite(SpriteRenderer sprite)
    {
        sourceSprite = sprite;
        SetupMaterial();
        AllocateBuffers();
        RebuildMesh();
    }

    private void SetupMaterial()
    {
        if (sourceSprite == null || sourceSprite.sprite == null) return;

        // Create a dedicated material with the sprite texture explicitly set
        var spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (spriteShader == null)
            spriteShader = Shader.Find("Sprites/Default");

        if (_instanceMaterial != null)
            Destroy(_instanceMaterial);

        _instanceMaterial = new Material(spriteShader);
        _instanceMaterial.mainTexture = sourceSprite.sprite.texture;
        _meshRenderer.material = _instanceMaterial;

        // Copy sorting settings from source sprite
        _meshRenderer.sortingLayerID = sourceSprite.sortingLayerID;
        _meshRenderer.sortingOrder = sourceSprite.sortingOrder;
    }

    private void AllocateBuffers()
    {
        int vertCount = (segments + 1) * 2; // top row + bottom row
        _vertices = new Vector3[vertCount];
        _uvs = new Vector2[vertCount];
        _colors = new Color[vertCount];
        _groundHeights = new float[segments + 1];
        _smoothBuffer = new float[segments + 1];
        _hitXPositions = new float[segments + 1];
        _triangles = new int[segments * 6]; // 2 tris per segment

        // Build triangle indices (stays constant)
        for (int i = 0; i < segments; i++)
        {
            int bottomLeft = i;
            int bottomRight = i + 1;
            int topLeft = (segments + 1) + i;
            int topRight = (segments + 1) + i + 1;

            int ti = i * 6;
            _triangles[ti + 0] = bottomLeft;
            _triangles[ti + 1] = topLeft;
            _triangles[ti + 2] = bottomRight;

            _triangles[ti + 3] = bottomRight;
            _triangles[ti + 4] = topLeft;
            _triangles[ti + 5] = topRight;
        }
    }

    private void RebuildMesh()
    {
        if (sourceSprite == null || sourceSprite.sprite == null) return;

        // Use sprite rect (full canvas size) instead of bounds (alpha-clipped)
        // so animated sprites with the same canvas size stay consistent
        Sprite sp = sourceSprite.sprite;
        float ppu = sp.pixelsPerUnit;
        float fullWidth = sp.rect.width / ppu;
        float fullHeight = sp.rect.height / ppu;

        float spriteWidth = widthOverride > 0f ? widthOverride : fullWidth;
        float halfWidth = spriteWidth * 0.5f;

        float spriteAspect = fullHeight / Mathf.Max(fullWidth, 0.001f);
        float meshHeight = spriteWidth * spriteAspect * heightScale;

        Vector3 center = transform.position;
        Color spriteColor = sourceSprite.color;

        // Handle sprite atlas tight-packing:
        // textureRect = trimmed region in the atlas (no transparent borders)
        // rect = full original frame (with transparent borders)
        // textureRectOffset = where the trimmed content sits within the full frame
        Texture2D tex = sp.texture;
        Rect texRect = sp.textureRect;
        Vector2 texRectOffset = sp.textureRectOffset;

        // Size of one pixel in UV space
        float pixelW = 1f / tex.width;
        float pixelH = 1f / tex.height;

        // UV rect of the trimmed content in the atlas
        float trimUvLeft   = texRect.x * pixelW;
        float trimUvBottom = texRect.y * pixelH;
        float trimUvW      = texRect.width * pixelW;
        float trimUvH      = texRect.height * pixelH;

        // Expand to represent the full frame (rect) including transparent borders
        // textureRectOffset is how far the trimmed content is from the bottom-left of the full rect
        float fullRectW = sp.rect.width;
        float fullRectH = sp.rect.height;

        float uvLeft   = trimUvLeft   - texRectOffset.x * pixelW;
        float uvBottom = trimUvBottom - texRectOffset.y * pixelH;
        float uvRight  = uvLeft   + fullRectW * pixelW;
        float uvTop    = uvBottom + fullRectH * pixelH;

        // Cast rays from a single center point, fanning outward
        Vector3 rayOrigin = center + new Vector3(0f, raycastHeight, 0f);
        float halfSpreadRad = spreadAngle * Mathf.Deg2Rad;

        bool[] hasHit = new bool[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            // Fan angle: from -spreadAngle to +spreadAngle
            float angle = Mathf.Lerp(-halfSpreadRad, halfSpreadRad, t);
            // Ray direction: rotate downward vector by angle
            Vector2 dir = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)).normalized;

            if (Physics2D.Raycast(rayOrigin, dir, rayMaxDistance, groundLayer) is var hit && hit.collider != null)
            {
                _groundHeights[i] = hit.point.y + surfaceOffset - center.y;
                _hitXPositions[i] = hit.point.x - center.x;
                hasHit[i] = true;
            }
            else
            {
                _groundHeights[i] = 0f;
                _hitXPositions[i] = Mathf.Lerp(-halfWidth, halfWidth, t);
                hasHit[i] = false;
            }
        }

        // Extrapolate missed edges from the nearest two valid hits
        // Find first and last valid indices
        int firstValid = -1, secondValid = -1;
        int lastValid = -1, secondLastValid = -1;
        for (int i = 0; i <= segments; i++)
        {
            if (hasHit[i])
            {
                if (firstValid < 0) firstValid = i;
                else if (secondValid < 0) secondValid = i;
            }
        }
        for (int i = segments; i >= 0; i--)
        {
            if (hasHit[i])
            {
                if (lastValid < 0) lastValid = i;
                else if (secondLastValid < 0) secondLastValid = i;
            }
        }

        // Extrapolate left edge misses (both Y heights and X positions)
        if (firstValid >= 0 && secondValid >= 0)
        {
            float slopeY = (_groundHeights[secondValid] - _groundHeights[firstValid]) / (secondValid - firstValid);
            float slopeX = (_hitXPositions[secondValid] - _hitXPositions[firstValid]) / (secondValid - firstValid);
            for (int i = firstValid - 1; i >= 0; i--)
            {
                _groundHeights[i] = _groundHeights[firstValid] + slopeY * (i - firstValid);
                _hitXPositions[i] = _hitXPositions[firstValid] + slopeX * (i - firstValid);
            }
        }
        else if (firstValid >= 0)
        {
            float xStep = (firstValid > 0) ? (_hitXPositions[firstValid] / firstValid) : 0f;
            for (int i = firstValid - 1; i >= 0; i--)
            {
                _groundHeights[i] = _groundHeights[firstValid];
                _hitXPositions[i] = _hitXPositions[firstValid] + xStep * (i - firstValid);
            }
        }

        // Extrapolate right edge misses (both Y heights and X positions)
        if (lastValid >= 0 && secondLastValid >= 0)
        {
            float slopeY = (_groundHeights[lastValid] - _groundHeights[secondLastValid]) / (lastValid - secondLastValid);
            float slopeX = (_hitXPositions[lastValid] - _hitXPositions[secondLastValid]) / (lastValid - secondLastValid);
            for (int i = lastValid + 1; i <= segments; i++)
            {
                _groundHeights[i] = _groundHeights[lastValid] + slopeY * (i - lastValid);
                _hitXPositions[i] = _hitXPositions[lastValid] + slopeX * (i - lastValid);
            }
        }
        else if (lastValid >= 0)
        {
            float xStep = (lastValid < segments) ? (_hitXPositions[lastValid] / lastValid) : 0f;
            for (int i = lastValid + 1; i <= segments; i++)
            {
                _groundHeights[i] = _groundHeights[lastValid];
                _hitXPositions[i] = _hitXPositions[lastValid] + xStep * (i - lastValid);
            }
        }

        // Smooth ground heights to reduce sharp differences between segments
        for (int pass = 0; pass < smoothingPasses; pass++)
        {
            for (int i = 0; i <= segments; i++)
            {
                float prev = _groundHeights[Mathf.Max(i - 1, 0)];
                float curr = _groundHeights[i];
                float next = _groundHeights[Mathf.Min(i + 1, segments)];
                _smoothBuffer[i] = (prev + curr + next) / 3f;
            }

            // Copy smoothed values back for next pass
            var tmp = _groundHeights;
            _groundHeights = _smoothBuffer;
            _smoothBuffer = tmp;
        }

        // Force edge vertices to cover full sprite width
        // Left edge
        _hitXPositions[0] = -halfWidth;
        if (segments >= 2)
        {
            float dx = _hitXPositions[2] - _hitXPositions[1];
            float dy = _groundHeights[2] - _groundHeights[1];
            float edgeDx = -halfWidth - _hitXPositions[1];
            _groundHeights[0] = _groundHeights[1] + (dx != 0f ? dy / dx * edgeDx : 0f);
        }
        else
        {
            _groundHeights[0] = _groundHeights[Mathf.Min(1, segments)];
        }

        // Right edge
        _hitXPositions[segments] = halfWidth;
        if (segments >= 2)
        {
            float dx = _hitXPositions[segments - 1] - _hitXPositions[segments - 2];
            float dy = _groundHeights[segments - 1] - _groundHeights[segments - 2];
            float edgeDx = halfWidth - _hitXPositions[segments - 1];
            _groundHeights[segments] = _groundHeights[segments - 1] + (dx != 0f ? dy / dx * edgeDx : 0f);
        }
        else
        {
            _groundHeights[segments] = _groundHeights[Mathf.Max(segments - 1, 0)];
        }

        // Build vertices using hit X positions
        // Compute UV t from hit X relative to sprite width
        for (int i = 0; i <= segments; i++)
        {
            float localX = _hitXPositions[i];
            float localGroundY = _groundHeights[i];

            // Map UV based on where this vertex sits within the sprite width
            float uvT = Mathf.InverseLerp(-halfWidth, halfWidth, localX);
            uvT = Mathf.Clamp01(uvT);

            int bottomIdx = i;
            _vertices[bottomIdx] = new Vector3(localX, localGroundY, 0f);
            _uvs[bottomIdx] = new Vector2(Mathf.Lerp(uvLeft, uvRight, uvT), uvBottom);
            _colors[bottomIdx] = spriteColor;

            int topIdx = (segments + 1) + i;
            _vertices[topIdx] = new Vector3(localX, localGroundY + meshHeight, 0f);
            _uvs[topIdx] = new Vector2(Mathf.Lerp(uvLeft, uvRight, uvT), uvTop);
            _colors[topIdx] = spriteColor;
        }

        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.uv = _uvs;
        _mesh.triangles = _triangles;
        _mesh.colors = _colors;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (_mesh != null)
            Destroy(_mesh);
        if (_instanceMaterial != null)
            Destroy(_instanceMaterial);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (sourceSprite == null) return;

        Vector3 center = transform.position;
        Vector3 origin = center + new Vector3(0f, raycastHeight, 0f);
        float halfSpreadRad = spreadAngle * Mathf.Deg2Rad;

        // Draw ray fan
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, 0.05f);
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(-halfSpreadRad, halfSpreadRad, t);
            Vector3 dir = new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0f).normalized;

            Gizmos.DrawLine(origin, origin + dir * rayMaxDistance);
        }

        // Draw mesh outline preview
        if (_vertices != null && _vertices.Length > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < segments; i++)
            {
                // Bottom edge
                Gizmos.DrawLine(center + _vertices[i], center + _vertices[i + 1]);
                // Top edge
                int topI = (segments + 1) + i;
                Gizmos.DrawLine(center + _vertices[topI], center + _vertices[topI + 1]);
                // Vertical edges
                Gizmos.DrawLine(center + _vertices[i], center + _vertices[topI]);
            }
            // Last vertical
            Gizmos.DrawLine(center + _vertices[segments], center + _vertices[(segments + 1) + segments]);
        }
    }
#endif
}
