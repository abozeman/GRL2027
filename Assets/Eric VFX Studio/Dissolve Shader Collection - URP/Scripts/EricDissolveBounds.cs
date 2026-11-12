using UnityEngine;

namespace EricVFX
{
    /// <summary>
    /// Supplies per-character dissolve bounds/center values through MaterialPropertyBlock.
    /// Multiple characters can share the same Material while keeping independent dissolve bounds.
    ///
    /// Supported shader properties:
    /// _WorldHeightMin / _WorldHeightMax
    /// _WorldXMin      / _WorldXMax
    /// _CenterX        / _CenterY
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class EricDissolveBounds : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("Automatically uses all Renderers under this GameObject.")]
        [SerializeField] private bool includeInactiveRenderers = true;

        [Header("Screen Space")]
        [Tooltip("Optional camera used for Screen Space dissolve. If empty, Camera.main is used.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Update the Screen Space center every frame. Keep enabled if the camera or character can move.")]
        [SerializeField] private bool updateScreenCenter = true;

        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;

        private static readonly int WorldHeightMinID = Shader.PropertyToID("_WorldHeightMin");
        private static readonly int WorldHeightMaxID = Shader.PropertyToID("_WorldHeightMax");
        private static readonly int WorldXMinID = Shader.PropertyToID("_WorldXMin");
        private static readonly int WorldXMaxID = Shader.PropertyToID("_WorldXMax");
        private static readonly int CenterXID = Shader.PropertyToID("_CenterX");
        private static readonly int CenterYID = Shader.PropertyToID("_CenterY");

        private void OnEnable()
        {
            propertyBlock = new MaterialPropertyBlock();
            RefreshRenderers();
            ApplyAllProperties();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            RefreshRenderers();
            ApplyAllProperties();
        }

        private void OnTransformChildrenChanged()
        {
            RefreshRenderers();
            ApplyAllProperties();
        }

        private void LateUpdate()
        {
            if (renderers == null || renderers.Length == 0)
                RefreshRenderers();

            // World bounds can change on animated SkinnedMeshRenderers,
            // so update them continuously.
            ApplyWorldBounds();

            if (updateScreenCenter)
                ApplyScreenCenter();
        }

        [ContextMenu("Refresh Renderers")]
        public void RefreshRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>(includeInactiveRenderers);
        }

        [ContextMenu("Apply Dissolve Bounds Now")]
        public void ApplyAllProperties()
        {
            if (renderers == null || renderers.Length == 0)
                RefreshRenderers();

            ApplyWorldBounds();
            ApplyScreenCenter();
        }

        private bool TryGetCombinedBounds(out Bounds combinedBounds)
        {
            combinedBounds = default;

            if (renderers == null || renderers.Length == 0)
                return false;

            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null)
                    continue;

                if (!includeInactiveRenderers && !renderer.gameObject.activeInHierarchy)
                    continue;

                if (!hasBounds)
                {
                    combinedBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private void ApplyWorldBounds()
        {
            if (!TryGetCombinedBounds(out Bounds bounds))
                return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null)
                    continue;

                propertyBlock.Clear();
                renderer.GetPropertyBlock(propertyBlock);

                propertyBlock.SetFloat(WorldHeightMinID, bounds.min.y);
                propertyBlock.SetFloat(WorldHeightMaxID, bounds.max.y);

                propertyBlock.SetFloat(WorldXMinID, bounds.min.x);
                propertyBlock.SetFloat(WorldXMaxID, bounds.max.x);

                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ApplyScreenCenter()
        {
            if (!TryGetCombinedBounds(out Bounds bounds))
                return;

            Camera cam = targetCamera != null ? targetCamera : Camera.main;

            if (cam == null)
                return;

            Vector3 viewportPoint = cam.WorldToViewportPoint(bounds.center);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null)
                    continue;

                propertyBlock.Clear();
                renderer.GetPropertyBlock(propertyBlock);

                propertyBlock.SetFloat(CenterXID, viewportPoint.x);
                propertyBlock.SetFloat(CenterYID, viewportPoint.y);

                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
