using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Platformer {
    public enum BarrierPosterFace {
        Forward,
        Back,
        Right,
        Left,
        Top,
        Bottom
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Environment/Invisible Image Barrier")]
    public class InvisibleImageBarrier : MonoBehaviour {
        [Header("Barrier")]
        [SerializeField] private Vector3 barrierSize = new Vector3(4f, 3f, 0.8f);
        [SerializeField] private Vector3 barrierCenter;
        [SerializeField] private bool blockPlayer = true;
        [SerializeField] private bool forceDefaultPhysicsLayer = true;
        [SerializeField] private bool addKinematicRigidbody = true;
        [Tooltip("Creates only the side walls by default. Turn on if you also need ceiling and floor limits.")]
        [SerializeField] private bool blockTopAndBottom;
        [SerializeField] private float minimumBlockingThickness = 0.35f;
        [SerializeField] private PhysicsMaterial colliderMaterial;

        [Header("Reveal")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float revealDistance = 3f;
        [SerializeField] private float hideDistance = 4f;
        [SerializeField] private float fadeSpeed = 5f;
        [Tooltip("If enabled, the poster appears when the player is close to any side of the barrier, not only to Poster Face.")]
        [SerializeField] private bool revealNearAnyWall = true;
        [Tooltip("Debug option for checking placement and texture in Play Mode.")]
        [SerializeField] private bool alwaysShowPosterInPlayMode;

        [Header("Reveal Effect")]
        [Tooltip("Texture covers the whole barrier face and stays hidden until the player is near.")]
        [SerializeField] private bool coverEntireFace = true;
        [Tooltip("Tile the texture on every side of the barrier instead of only on Poster Face.")]
        [SerializeField] private bool posterOnAllFaces = true;
        [Tooltip("Also tile the texture on the top and bottom faces.")]
        [SerializeField] private bool posterOnTopAndBottom;
        [Tooltip("Radius (world units) of the circle that reveals the texture around the player.")]
        [SerializeField] private float revealRadius = 1.5f;
        [Tooltip("Width of the soft fade at the edge of the reveal circle. Higher = smoother edge.")]
        [SerializeField] private float revealSoftness = 0.9f;
        [Tooltip("World-space size of one copy of the image. With Cover Entire Face on, the image keeps this size and repeats to fill the wall (Y is ignored when Keep Image Aspect is on).")]
        [SerializeField] private Vector2 tileSize = new Vector2(1f, 1f);

        [Header("Poster")]
        [Tooltip("Drag a PNG imported as Sprite here.")]
        [SerializeField] private Sprite posterSprite;
        [Tooltip("Optional fallback if the PNG is imported as a Default Texture.")]
        [SerializeField] private Texture2D posterTexture;
        [SerializeField] private BarrierPosterFace posterFace = BarrierPosterFace.Forward;
        [SerializeField] private Vector2 posterSize = new Vector2(1.7f, 1.05f);
        [SerializeField] private Vector2 posterOffset;
        [SerializeField] private bool keepImageAspect = true;
        [SerializeField, Range(0.05f, 1f)] private float maxFaceCoverage = 0.65f;
        [SerializeField] private float surfaceOffset = 0.04f;
        [SerializeField] private Color posterTint = Color.white;
        [SerializeField] private int sortingOrder = 20;
        [SerializeField] private bool showPosterPreviewInEditor = true;

        [Header("Gizmo")]
        [SerializeField] private bool drawGizmo = true;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.7f, 1f, 0.22f);
        [SerializeField] private Color gizmoWireColor = new Color(0.2f, 0.9f, 1f, 0.85f);

        const string PosterObjectName = "Barrier Poster";
        const string WallObjectPrefix = "Barrier Wall ";
        const string RevealShaderName = "Custom/BarrierReveal";
        const float EditorPreviewRevealRadius = 100000f;
        const int DefaultPhysicsLayer = 0;
        static readonly BarrierPosterFace[] AllFaces = {
            BarrierPosterFace.Forward,
            BarrierPosterFace.Back,
            BarrierPosterFace.Right,
            BarrierPosterFace.Left,
            BarrierPosterFace.Top,
            BarrierPosterFace.Bottom
        };

        BoxCollider barrierCollider;
        Rigidbody barrierRigidbody;
        Transform player;
        MeshFilter posterMeshFilter;
        MeshRenderer posterMeshRenderer;
        Mesh posterMesh;
        Material posterMaterial;
        bool revealShaderActive;
        float currentAlpha;
        float fadeVelocity;
        bool revealed;
        readonly List<Vector3> posterVertices = new List<Vector3>();
        readonly List<Vector2> posterUvs = new List<Vector2>();
        readonly List<int> posterTriangles = new List<int>();

        void Reset() {
            barrierSize = new Vector3(4f, 3f, 0.8f);
            posterSize = new Vector2(1.7f, 1.05f);
            revealDistance = 3f;
            hideDistance = 4f;
            blockPlayer = true;
            addKinematicRigidbody = true;
            forceDefaultPhysicsLayer = true;
            blockTopAndBottom = false;
            revealNearAnyWall = true;
            coverEntireFace = true;
            posterOnAllFaces = true;
            posterOnTopAndBottom = false;
            revealRadius = 1.5f;
            revealSoftness = 0.9f;
            tileSize = new Vector2(1f, 1f);
        }

        void Awake() {
            RebuildBarrier();
            currentAlpha = 0f;
            ApplyBaselineVisual();
        }

        void OnEnable() {
            RebuildBarrier();
        }

        void Start() {
            if (Application.isPlaying)
                FindPlayer();
        }

        void Update() {
            RebuildBarrier();

            if (!Application.isPlaying) {
                ApplyEditorPreview();
                return;
            }

            if (player == null)
                FindPlayer();

            if (alwaysShowPosterInPlayMode)
                revealed = true;
            else
                UpdateRevealState();

            float targetAlpha = revealed && ResolvePosterTexture() != null ? 1f : 0f;
            // SmoothDamp eases in and out, so the fade has no hard start/stop like MoveTowards.
            float smoothTime = 1f / Mathf.Max(0.1f, fadeSpeed);
            currentAlpha = Mathf.Clamp01(Mathf.SmoothDamp(currentAlpha, targetAlpha, ref fadeVelocity, smoothTime));
            UpdatePosterVisual(currentAlpha, GetRevealCenter(), revealRadius);
        }

        void OnValidate() {
            NormalizeSettings();
            RebuildBarrier();
        }

        void RebuildBarrier() {
            NormalizeSettings();
            EnsurePhysicsLayer();
            EnsureCollider();
            EnsureWallColliders();
            EnsureRigidbody();
            EnsurePoster();
        }

        void NormalizeSettings() {
            barrierSize = new Vector3(
                Mathf.Max(0.05f, barrierSize.x),
                Mathf.Max(0.05f, barrierSize.y),
                Mathf.Max(0.05f, barrierSize.z));
            minimumBlockingThickness = Mathf.Max(0.05f, minimumBlockingThickness);
            posterSize = new Vector2(Mathf.Max(0.05f, posterSize.x), Mathf.Max(0.05f, posterSize.y));
            revealDistance = Mathf.Max(0.1f, revealDistance);
            hideDistance = Mathf.Max(revealDistance, hideDistance);
            fadeSpeed = Mathf.Max(0.1f, fadeSpeed);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
            revealRadius = Mathf.Max(0f, revealRadius);
            revealSoftness = Mathf.Max(0.0001f, revealSoftness);
            tileSize = new Vector2(Mathf.Max(0.01f, tileSize.x), Mathf.Max(0.01f, tileSize.y));
        }

        void EnsurePhysicsLayer() {
            if (!forceDefaultPhysicsLayer)
                return;

            gameObject.layer = DefaultPhysicsLayer;
        }

        void EnsureCollider() {
            if (barrierCollider == null)
                barrierCollider = GetComponent<BoxCollider>();
            if (barrierCollider == null)
                return;

            barrierCollider.enabled = false;
            barrierCollider.center = barrierCenter;
            barrierCollider.size = barrierSize;
            barrierCollider.isTrigger = true;
            barrierCollider.sharedMaterial = colliderMaterial;
        }

        void EnsureWallColliders() {
            EnsureWallCollider(BarrierPosterFace.Forward);
            EnsureWallCollider(BarrierPosterFace.Back);
            EnsureWallCollider(BarrierPosterFace.Right);
            EnsureWallCollider(BarrierPosterFace.Left);
            EnsureWallCollider(BarrierPosterFace.Top);
            EnsureWallCollider(BarrierPosterFace.Bottom);
        }

        void EnsureWallCollider(BarrierPosterFace face) {
            Transform wall = transform.Find(GetWallObjectName(face));
            if (wall == null) {
                var wallObject = new GameObject(GetWallObjectName(face));
                wall = wallObject.transform;
                wall.SetParent(transform, false);
            }

            if (forceDefaultPhysicsLayer)
                wall.gameObject.layer = DefaultPhysicsLayer;

            BoxCollider wallCollider = wall.GetComponent<BoxCollider>();
            if (wallCollider == null)
                wallCollider = wall.gameObject.AddComponent<BoxCollider>();

            wall.localPosition = ResolveWallCenter(face);
            wall.localRotation = Quaternion.identity;
            wall.localScale = Vector3.one;

            wallCollider.enabled = ShouldBlockFace(face);
            wallCollider.isTrigger = false;
            wallCollider.center = Vector3.zero;
            wallCollider.size = ResolveWallSize(face);
            wallCollider.sharedMaterial = colliderMaterial;
        }

        bool ShouldBlockFace(BarrierPosterFace face) {
            if (!blockPlayer)
                return false;

            switch (face) {
                case BarrierPosterFace.Top:
                case BarrierPosterFace.Bottom:
                    return blockTopAndBottom;
                default:
                    return true;
            }
        }

        Vector3 ResolveWallCenter(BarrierPosterFace face) {
            Vector3 center = barrierCenter;
            Vector3 half = barrierSize * 0.5f;

            switch (face) {
                case BarrierPosterFace.Forward:
                    center.z += half.z;
                    break;
                case BarrierPosterFace.Back:
                    center.z -= half.z;
                    break;
                case BarrierPosterFace.Right:
                    center.x += half.x;
                    break;
                case BarrierPosterFace.Left:
                    center.x -= half.x;
                    break;
                case BarrierPosterFace.Top:
                    center.y += half.y;
                    break;
                case BarrierPosterFace.Bottom:
                    center.y -= half.y;
                    break;
            }

            return center;
        }

        Vector3 ResolveWallSize(BarrierPosterFace face) {
            float thickness = Mathf.Max(0.05f, minimumBlockingThickness);
            switch (face) {
                case BarrierPosterFace.Forward:
                case BarrierPosterFace.Back:
                    return new Vector3(barrierSize.x, barrierSize.y, thickness);
                case BarrierPosterFace.Right:
                case BarrierPosterFace.Left:
                    return new Vector3(thickness, barrierSize.y, barrierSize.z);
                case BarrierPosterFace.Top:
                case BarrierPosterFace.Bottom:
                    return new Vector3(barrierSize.x, thickness, barrierSize.z);
                default:
                    return barrierSize;
            }
        }

        static string GetWallObjectName(BarrierPosterFace face) {
            return WallObjectPrefix + face;
        }

        void EnsureRigidbody() {
            if (!addKinematicRigidbody || !blockPlayer)
                return;

            if (barrierRigidbody == null)
                barrierRigidbody = GetComponent<Rigidbody>();
            if (barrierRigidbody == null)
                barrierRigidbody = gameObject.AddComponent<Rigidbody>();

            barrierRigidbody.isKinematic = true;
            barrierRigidbody.useGravity = false;
            barrierRigidbody.detectCollisions = true;
        }

        void EnsurePoster() {
            Transform poster = transform.Find(PosterObjectName);
            if (poster == null) {
                var posterObject = new GameObject(PosterObjectName);
                poster = posterObject.transform;
                poster.SetParent(transform, false);
            }

            if (forceDefaultPhysicsLayer)
                poster.gameObject.layer = DefaultPhysicsLayer;

            DisableLegacySpritePoster(poster);
            if (!EnsurePosterMeshComponents(poster))
                return;

            Texture2D texture = ResolvePosterTexture();
            UpdatePosterTransform(poster);
            UpdatePosterMesh(texture);
            UpdatePosterMaterial(texture);
            posterMeshRenderer.sortingOrder = sortingOrder;
            posterMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            posterMeshRenderer.receiveShadows = false;
            posterMeshRenderer.allowOcclusionWhenDynamic = false;
            ApplyBaselineVisual();
        }

        void DisableLegacySpritePoster(Transform poster) {
            if (poster == null)
                return;

            SpriteRenderer legacySpriteRenderer = poster.GetComponent<SpriteRenderer>();
            if (legacySpriteRenderer == null)
                return;

            legacySpriteRenderer.enabled = false;

            // A MeshRenderer can't be added while a SpriteRenderer exists (both derive from
            // Renderer). Older versions of this component left a SpriteRenderer on the poster,
            // so it has to be removed. Destruction is deferred because Unity forbids it during
            // Awake / OnEnable / OnValidate; the rebuild re-runs afterwards and adds the mesh.
            if (Application.isPlaying) {
                Destroy(legacySpriteRenderer);
                return;
            }

#if UNITY_EDITOR
            SpriteRenderer pending = legacySpriteRenderer;
            UnityEditor.EditorApplication.delayCall += () => {
                if (pending != null)
                    DestroyImmediate(pending);
                if (this != null)
                    RebuildBarrier();
            };
#endif
        }

        bool EnsurePosterMeshComponents(Transform poster) {
            if (poster == null)
                return false;

            // A SpriteRenderer still on the poster (being removed by DisableLegacySpritePoster)
            // would make AddComponent<MeshRenderer> fail. Wait until it is gone.
            if (poster.GetComponent<SpriteRenderer>() != null)
                return false;

            if (posterMeshFilter == null)
                posterMeshFilter = poster.GetComponent<MeshFilter>();
            if (posterMeshFilter == null)
                posterMeshFilter = poster.gameObject.AddComponent<MeshFilter>();

            if (posterMeshRenderer == null)
                posterMeshRenderer = poster.GetComponent<MeshRenderer>();
            if (posterMeshRenderer == null)
                posterMeshRenderer = poster.gameObject.AddComponent<MeshRenderer>();

            // During scene load / prefab instantiation Unity can refuse AddComponent and
            // return null without throwing. Bail out gracefully; Update() re-runs the
            // rebuild every frame, so the poster is created once the engine allows it.
            if (posterMeshFilter == null || posterMeshRenderer == null)
                return false;

            if (posterMesh == null) {
                posterMesh = new Mesh { name = "Invisible Barrier Poster Mesh" };
                posterMesh.hideFlags = HideFlags.HideAndDontSave;
            }

            posterMeshFilter.sharedMesh = posterMesh;
            return true;
        }

        Texture2D ResolvePosterTexture() {
            if (posterSprite != null && posterSprite.texture != null)
                return posterSprite.texture;

            return posterTexture;
        }

        void UpdatePosterTransform(Transform poster) {
            if (coverEntireFace) {
                // The mesh is built in barrier-local space (it carries each face's position and
                // orientation itself), so the poster object stays at the barrier's origin.
                poster.localPosition = Vector3.zero;
                poster.localRotation = Quaternion.identity;
                poster.localScale = Vector3.one;
                return;
            }

            Quaternion rotation = GetFaceRotation();
            Vector3 position = GetFaceCenter();
            position += rotation * new Vector3(posterOffset.x, posterOffset.y, 0f);

            poster.localPosition = position;
            poster.localRotation = rotation;
            poster.localScale = Vector3.one;
        }

        void UpdatePosterMesh(Texture2D texture) {
            if (posterMesh == null)
                return;

            posterMesh.Clear();

            if (coverEntireFace) {
                BuildFacesMesh();
                return;
            }

            BuildSinglePosterMesh(texture);
        }

        void BuildSinglePosterMesh(Texture2D texture) {
            Vector2 size = ResolvePosterSize();
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;

            posterMesh.vertices = new[] {
                new Vector3(-halfWidth, -halfHeight, 0f),
                new Vector3(halfWidth, -halfHeight, 0f),
                new Vector3(halfWidth, halfHeight, 0f),
                new Vector3(-halfWidth, halfHeight, 0f)
            };
            posterMesh.uv = ResolvePosterUvs(texture);
            posterMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            posterMesh.RecalculateBounds();
            posterMesh.RecalculateNormals();
        }

        void BuildFacesMesh() {
            posterVertices.Clear();
            posterUvs.Clear();
            posterTriangles.Clear();

            foreach (BarrierPosterFace face in GetPosterFaces())
                AppendFaceQuad(face);

            posterMesh.SetVertices(posterVertices);
            posterMesh.SetUVs(0, posterUvs);
            posterMesh.SetTriangles(posterTriangles, 0);
            posterMesh.RecalculateBounds();
            posterMesh.RecalculateNormals();
        }

        void AppendFaceQuad(BarrierPosterFace face) {
            Vector3 half = barrierSize * 0.5f;
            Vector3 normal, right, up;
            float depth;
            Vector2 size;

            switch (face) {
                case BarrierPosterFace.Back:
                    normal = Vector3.back; right = Vector3.right; up = Vector3.up;
                    depth = half.z; size = new Vector2(barrierSize.x, barrierSize.y); break;
                case BarrierPosterFace.Right:
                    normal = Vector3.right; right = Vector3.forward; up = Vector3.up;
                    depth = half.x; size = new Vector2(barrierSize.z, barrierSize.y); break;
                case BarrierPosterFace.Left:
                    normal = Vector3.left; right = Vector3.forward; up = Vector3.up;
                    depth = half.x; size = new Vector2(barrierSize.z, barrierSize.y); break;
                case BarrierPosterFace.Top:
                    normal = Vector3.up; right = Vector3.right; up = Vector3.forward;
                    depth = half.y; size = new Vector2(barrierSize.x, barrierSize.z); break;
                case BarrierPosterFace.Bottom:
                    normal = Vector3.down; right = Vector3.right; up = Vector3.forward;
                    depth = half.y; size = new Vector2(barrierSize.x, barrierSize.z); break;
                default: // Forward
                    normal = Vector3.forward; right = Vector3.right; up = Vector3.up;
                    depth = half.z; size = new Vector2(barrierSize.x, barrierSize.y); break;
            }

            Vector3 center = barrierCenter + normal * (depth + surfaceOffset);
            Vector3 r = right * (size.x * 0.5f);
            Vector3 u = up * (size.y * 0.5f);

            int baseIndex = posterVertices.Count;
            posterVertices.Add(center - r - u);
            posterVertices.Add(center + r - u);
            posterVertices.Add(center + r + u);
            posterVertices.Add(center - r + u);

            Vector2 tiling = GetTextureTiling(size);
            posterUvs.Add(new Vector2(0f, 0f));
            posterUvs.Add(new Vector2(tiling.x, 0f));
            posterUvs.Add(new Vector2(tiling.x, tiling.y));
            posterUvs.Add(new Vector2(0f, tiling.y));

            // The shader is unlit and double-sided (Cull Off), so winding/normals don't matter.
            posterTriangles.Add(baseIndex + 0);
            posterTriangles.Add(baseIndex + 1);
            posterTriangles.Add(baseIndex + 2);
            posterTriangles.Add(baseIndex + 0);
            posterTriangles.Add(baseIndex + 2);
            posterTriangles.Add(baseIndex + 3);
        }

        IEnumerable<BarrierPosterFace> GetPosterFaces() {
            if (!posterOnAllFaces) {
                yield return posterFace;
                yield break;
            }

            yield return BarrierPosterFace.Forward;
            yield return BarrierPosterFace.Back;
            yield return BarrierPosterFace.Right;
            yield return BarrierPosterFace.Left;

            if (posterOnTopAndBottom) {
                yield return BarrierPosterFace.Top;
                yield return BarrierPosterFace.Bottom;
            }
        }

        // Keeps each copy of the image at a fixed world size and repeats it across the given face.
        Vector2 GetTextureTiling(Vector2 face) {
            Vector2 copySize = tileSize;
            float aspect = GetPosterAspect();
            if (keepImageAspect && aspect > 0.001f)
                copySize.y = copySize.x / aspect;
            copySize.x = Mathf.Max(0.01f, copySize.x);
            copySize.y = Mathf.Max(0.01f, copySize.y);
            return new Vector2(face.x / copySize.x, face.y / copySize.y);
        }

        Vector2[] ResolvePosterUvs(Texture2D texture) {
            if (posterSprite == null || texture == null || posterSprite.texture != texture || texture.width <= 0 || texture.height <= 0) {
                return new[] {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f)
                };
            }

            Rect rect = posterSprite.rect;
            float xMin = rect.xMin / texture.width;
            float xMax = rect.xMax / texture.width;
            float yMin = rect.yMin / texture.height;
            float yMax = rect.yMax / texture.height;

            return new[] {
                new Vector2(xMin, yMin),
                new Vector2(xMax, yMin),
                new Vector2(xMax, yMax),
                new Vector2(xMin, yMax)
            };
        }

        void UpdatePosterMaterial(Texture2D texture) {
            if (posterMeshRenderer == null)
                return;

            if (posterMaterial == null) {
                Shader revealShader = Shader.Find(RevealShaderName);
                Shader shader = revealShader;
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Transparent");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Standard");

                revealShaderActive = revealShader != null && shader == revealShader;
                posterMaterial = new Material(shader) {
                    name = "Invisible Barrier Poster Material",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            // Tiling needs Repeat wrapping; .mainTexture is avoided because the reveal shader
            // exposes the texture as _BaseMap, not _MainTex.
            if (texture != null && coverEntireFace && texture.wrapMode != TextureWrapMode.Repeat)
                texture.wrapMode = TextureWrapMode.Repeat;

            posterMeshRenderer.sharedMaterial = posterMaterial;
            SetMaterialTexture("_BaseMap", texture);
            SetMaterialTexture("_MainTex", texture);

            // The reveal shader already sets up transparent blending in ShaderLab; only the
            // generic fallback materials need their transparency configured at runtime.
            if (!revealShaderActive)
                ConfigureTransparentMaterial();
        }

        void ConfigureTransparentMaterial() {
            if (posterMaterial == null)
                return;

            SetMaterialFloat("_Surface", 1f);
            SetMaterialFloat("_Mode", 3f);
            SetMaterialFloat("_Blend", 0f);
            SetMaterialFloat("_AlphaClip", 0f);
            SetMaterialFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            SetMaterialFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetMaterialFloat("_ZWrite", 0f);
            SetMaterialFloat("_Cull", (float)CullMode.Off);
            posterMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            posterMaterial.DisableKeyword("_ALPHATEST_ON");
            posterMaterial.renderQueue = (int)RenderQueue.Transparent;
        }

        void SetMaterialTexture(string propertyName, Texture texture) {
            if (posterMaterial != null && posterMaterial.HasProperty(propertyName))
                posterMaterial.SetTexture(propertyName, texture);
        }

        void SetMaterialFloat(string propertyName, float value) {
            if (posterMaterial != null && posterMaterial.HasProperty(propertyName))
                posterMaterial.SetFloat(propertyName, value);
        }

        void SetMaterialVector(string propertyName, Vector3 value) {
            if (posterMaterial != null && posterMaterial.HasProperty(propertyName))
                posterMaterial.SetVector(propertyName, value);
        }

        void SetMaterialColor(Color color) {
            if (posterMaterial == null)
                return;

            if (posterMaterial.HasProperty("_BaseColor"))
                posterMaterial.SetColor("_BaseColor", color);
            if (posterMaterial.HasProperty("_Color"))
                posterMaterial.SetColor("_Color", color);
        }

        Vector3 GetFaceCenter() {
            Vector3 center = barrierCenter;
            Vector3 half = barrierSize * 0.5f;
            float offset = surfaceOffset;

            switch (posterFace) {
                case BarrierPosterFace.Forward:
                    center.z += half.z + offset;
                    break;
                case BarrierPosterFace.Back:
                    center.z -= half.z + offset;
                    break;
                case BarrierPosterFace.Right:
                    center.x += half.x + offset;
                    break;
                case BarrierPosterFace.Left:
                    center.x -= half.x + offset;
                    break;
                case BarrierPosterFace.Top:
                    center.y += half.y + offset;
                    break;
                case BarrierPosterFace.Bottom:
                    center.y -= half.y + offset;
                    break;
            }

            return center;
        }

        Quaternion GetFaceRotation() {
            switch (posterFace) {
                case BarrierPosterFace.Back:
                    return Quaternion.Euler(0f, 180f, 0f);
                case BarrierPosterFace.Right:
                    return Quaternion.Euler(0f, 90f, 0f);
                case BarrierPosterFace.Left:
                    return Quaternion.Euler(0f, -90f, 0f);
                case BarrierPosterFace.Top:
                    return Quaternion.Euler(-90f, 0f, 0f);
                case BarrierPosterFace.Bottom:
                    return Quaternion.Euler(90f, 0f, 0f);
                default:
                    return Quaternion.identity;
            }
        }

        Vector2 ResolvePosterSize() {
            Vector2 size = posterSize;
            float aspect = GetPosterAspect();

            if (keepImageAspect && aspect > 0.001f) {
                float requestedAspect = size.x / size.y;
                if (requestedAspect > aspect)
                    size.x = size.y * aspect;
                else
                    size.y = size.x / aspect;
            }

            Vector2 faceSize = GetFaceSize();
            float maxWidth = Mathf.Max(0.05f, faceSize.x * maxFaceCoverage);
            float maxHeight = Mathf.Max(0.05f, faceSize.y * maxFaceCoverage);
            float scale = Mathf.Min(1f, maxWidth / size.x, maxHeight / size.y);
            return size * scale;
        }

        float GetPosterAspect() {
            if (posterSprite != null && posterSprite.rect.height > 0.001f)
                return posterSprite.rect.width / posterSprite.rect.height;
            if (posterTexture != null && posterTexture.height > 0)
                return posterTexture.width / (float)posterTexture.height;

            return 0f;
        }

        Vector2 GetFaceSize() {
            switch (posterFace) {
                case BarrierPosterFace.Right:
                case BarrierPosterFace.Left:
                    return new Vector2(barrierSize.z, barrierSize.y);
                case BarrierPosterFace.Top:
                case BarrierPosterFace.Bottom:
                    return new Vector2(barrierSize.x, barrierSize.z);
                default:
                    return new Vector2(barrierSize.x, barrierSize.y);
            }
        }

        void UpdateRevealState() {
            if (player == null) {
                revealed = false;
                return;
            }

            float distance = GetDistanceToBarrier(player.position);
            revealed = revealed ? distance <= hideDistance : distance <= revealDistance;
        }

        float GetDistanceToBarrier(Vector3 worldPosition) {
            if (revealNearAnyWall) {
                float nearestDistance = float.PositiveInfinity;

                foreach (BarrierPosterFace face in AllFaces) {
                    BoxCollider wall = GetWallCollider(face);
                    if (wall == null || !wall.enabled)
                        continue;

                    nearestDistance = Mathf.Min(nearestDistance, GetDistanceToCollider(wall, worldPosition));
                }

                if (!float.IsPositiveInfinity(nearestDistance))
                    return nearestDistance;

                return GetDistanceToBounds(worldPosition);
            }

            BoxCollider wallCollider = GetWallCollider(posterFace);
            if (wallCollider != null && wallCollider.enabled)
                return GetDistanceToCollider(wallCollider, worldPosition);

            return GetDistanceToBounds(worldPosition);
        }

        static float GetDistanceToCollider(Collider targetCollider, Vector3 worldPosition) {
            Vector3 closestPoint = targetCollider.ClosestPoint(worldPosition);
            return Vector3.Distance(worldPosition, closestPoint);
        }

        BoxCollider GetWallCollider(BarrierPosterFace face) {
            Transform wall = transform.Find(GetWallObjectName(face));
            return wall != null ? wall.GetComponent<BoxCollider>() : null;
        }

        float GetDistanceToBounds(Vector3 worldPosition) {
            Vector3 local = transform.InverseTransformPoint(worldPosition) - barrierCenter;
            Vector3 half = barrierSize * 0.5f;
            Vector3 outside = new Vector3(
                Mathf.Max(Mathf.Abs(local.x) - half.x, 0f),
                Mathf.Max(Mathf.Abs(local.y) - half.y, 0f),
                Mathf.Max(Mathf.Abs(local.z) - half.z, 0f));

            if (outside.sqrMagnitude > 0f)
                return outside.magnitude;

            return Mathf.Min(
                half.x - Mathf.Abs(local.x),
                half.y - Mathf.Abs(local.y),
                half.z - Mathf.Abs(local.z));
        }

        void FindPlayer() {
            if (!string.IsNullOrEmpty(playerTag)) {
                try {
                    GameObject taggedPlayer = GameObject.FindGameObjectWithTag(playerTag);
                    if (taggedPlayer != null) {
                        player = taggedPlayer.transform;
                        return;
                    }
                }
                catch (UnityException) {
                    // Missing tag. Fall back to movement controller lookup below.
                }
            }

            MovementBrain movementBrain = FindFirstObjectByType<MovementBrain>();
            if (movementBrain != null)
                player = movementBrain.transform;
        }

        void ApplyBaselineVisual() {
            if (Application.isPlaying)
                UpdatePosterVisual(currentAlpha, GetRevealCenter(), revealRadius);
            else
                ApplyEditorPreview();
        }

        void ApplyEditorPreview() {
            // In the editor there is no player, so preview the whole texture (huge radius)
            // to make placement and tiling easy to check.
            float preview = showPosterPreviewInEditor ? 1f : 0f;
            UpdatePosterVisual(preview, GetPosterWorldCenter(), EditorPreviewRevealRadius);
        }

        void UpdatePosterVisual(float globalAlpha, Vector3 revealCenter, float radius) {
            if (posterMeshRenderer == null)
                return;

            Texture2D texture = ResolvePosterTexture();
            globalAlpha = Mathf.Clamp01(globalAlpha);
            posterMeshRenderer.enabled = texture != null && globalAlpha > 0.001f;

            if (revealShaderActive) {
                SetMaterialColor(posterTint);
                SetMaterialVector("_PlayerPos", revealCenter);
                SetMaterialFloat("_RevealRadius", Mathf.Max(0f, radius));
                SetMaterialFloat("_RevealSoftness", Mathf.Max(0.0001f, revealSoftness));
                SetMaterialFloat("_GlobalAlpha", globalAlpha);
                return;
            }

            // Fallback shader has no circular reveal; fade the whole poster instead.
            Color color = posterTint;
            color.a = posterTint.a * globalAlpha;
            SetMaterialColor(color);
        }

        Transform GetPosterTransform() {
            if (posterMeshFilter != null)
                return posterMeshFilter.transform;
            return transform.Find(PosterObjectName);
        }

        Vector3 GetPosterWorldCenter() {
            Transform poster = GetPosterTransform();
            return poster != null ? poster.position : transform.position;
        }

        Vector3 GetRevealCenter() {
            // The shader measures distance from each fragment to this world point, so passing the
            // player's actual position makes the reveal a sphere: it carves a circle into whichever
            // face the player is close to, on any side of the barrier.
            if (player != null)
                return player.position;
            return GetPosterWorldCenter();
        }

        void OnDestroy() {
            DestroyGeneratedPosterResources();
        }

        void DestroyGeneratedPosterResources() {
            DestroyGeneratedObject(posterMesh);
            DestroyGeneratedObject(posterMaterial);
            posterMesh = null;
            posterMaterial = null;
        }

        static void DestroyGeneratedObject(Object generatedObject) {
            if (generatedObject == null)
                return;
            if (Application.isPlaying)
                Destroy(generatedObject);
            else
                DestroyImmediate(generatedObject);
        }

        void OnDrawGizmos() {
            if (!drawGizmo)
                return;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(barrierCenter, barrierSize);
            Gizmos.color = gizmoWireColor;
            Gizmos.DrawWireCube(barrierCenter, barrierSize);
            Gizmos.matrix = previousMatrix;
        }
    }
}
