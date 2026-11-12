using UnityEngine;

namespace EricVFX.DissolveTest
{
    /// <summary>
    /// 小範圍測試用：在 Play Mode 內自動控制材質的 _DissolveAmount。
    /// 可掛在角色或任意物件上，把 Renderer 拖進 targets。
    /// </summary>
    public class ERIC_DissolveAmountAnimator : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private Renderer[] targets;

        [Header("Dissolve")]
        [Range(0f, 1f)] public float dissolveAmount = 0f;
        public bool playOnStart = false;
        public bool pingPong = true;
        public float duration = 2f;

        private MaterialPropertyBlock propertyBlock;
        private float timer;

        private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            ApplyAmount();
        }

        private void Update()
        {
            if (!playOnStart || duration <= 0.01f)
            {
                ApplyAmount();
                return;
            }

            timer += Time.deltaTime;
            float t = timer / duration;
            dissolveAmount = pingPong ? Mathf.PingPong(t, 1f) : Mathf.Clamp01(t);
            ApplyAmount();
        }

        private void OnValidate()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
            ApplyAmount();
        }

        private void ApplyAmount()
        {
            if (targets == null) return;

            foreach (Renderer target in targets)
            {
                if (target == null) continue;
                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(DissolveAmountID, dissolveAmount);
                target.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
