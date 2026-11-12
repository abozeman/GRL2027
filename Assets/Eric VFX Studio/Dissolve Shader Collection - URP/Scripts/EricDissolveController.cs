using System.Collections;
using UnityEngine;

namespace EricVFX
{
    [DisallowMultipleComponent]
    public class EricDissolveController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform targetRoot;
        [SerializeField] private bool includeInactiveRenderers = true;

        [Header("Dissolve")]
        [Range(0f, 1f)]
        [SerializeField] private float dissolveAmount = 0f;

        [Min(0.01f)]
        [SerializeField] private float defaultDuration = 1f;

        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private Coroutine dissolveRoutine;

        private static readonly int DissolveAmountID =
            Shader.PropertyToID("_DissolveAmount");

        public float DissolveAmount => dissolveAmount;

        private void Awake()
        {
            CacheRenderers();
            propertyBlock = new MaterialPropertyBlock();
            ApplyDissolveAmount(dissolveAmount);
        }

        private void OnEnable()
        {
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            if (renderers == null || renderers.Length == 0)
                CacheRenderers();

            ApplyDissolveAmount(dissolveAmount);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            dissolveAmount = Mathf.Clamp01(dissolveAmount);
            defaultDuration = Mathf.Max(0.01f, defaultDuration);

            if (!Application.isPlaying)
                return;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            if (renderers == null || renderers.Length == 0)
                CacheRenderers();

            ApplyDissolveAmount(dissolveAmount);
        }
#endif

        [ContextMenu("Refresh Renderers")]
        public void RefreshRenderers()
        {
            CacheRenderers();
            ApplyDissolveAmount(dissolveAmount);
        }

        public void SetDissolve(float amount)
        {
            StopCurrentAnimation();

            dissolveAmount = Mathf.Clamp01(amount);
            ApplyDissolveAmount(dissolveAmount);
        }

        public void Dissolve()
        {
            Dissolve(defaultDuration);
        }

        public void Dissolve(float duration)
        {
            AnimateTo(1f, duration);
        }

        public void Restore()
        {
            Restore(defaultDuration);
        }

        public void Restore(float duration)
        {
            AnimateTo(0f, duration);
        }

        [ContextMenu("Dissolve Instant")]
        public void DissolveInstant()
        {
            SetDissolve(1f);
        }

        [ContextMenu("Restore Instant")]
        public void RestoreInstant()
        {
            SetDissolve(0f);
        }

        private void AnimateTo(float targetAmount, float duration)
        {
            StopCurrentAnimation();

            if (!isActiveAndEnabled)
            {
                dissolveAmount = Mathf.Clamp01(targetAmount);
                ApplyDissolveAmount(dissolveAmount);
                return;
            }

            dissolveRoutine = StartCoroutine(
                AnimateDissolve(targetAmount, Mathf.Max(0.01f, duration))
            );
        }

        private IEnumerator AnimateDissolve(float targetAmount, float duration)
        {
            float startAmount = dissolveAmount;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                dissolveAmount = Mathf.Lerp(
                    startAmount,
                    targetAmount,
                    Mathf.Clamp01(elapsed / duration)
                );

                ApplyDissolveAmount(dissolveAmount);
                yield return null;
            }

            dissolveAmount = Mathf.Clamp01(targetAmount);
            ApplyDissolveAmount(dissolveAmount);

            dissolveRoutine = null;
        }

        private void StopCurrentAnimation()
        {
            if (dissolveRoutine == null)
                return;

            StopCoroutine(dissolveRoutine);
            dissolveRoutine = null;
        }

        private void CacheRenderers()
        {
            Transform root = targetRoot != null ? targetRoot : transform;

            renderers = root.GetComponentsInChildren<Renderer>(
                includeInactiveRenderers
            );
        }

        private void ApplyDissolveAmount(float amount)
        {
            if (renderers == null || renderers.Length == 0)
                return;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(DissolveAmountID, amount);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
