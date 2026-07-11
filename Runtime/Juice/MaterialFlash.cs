using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Damage flash for mesh renderers via MaterialPropertyBlock — no material instancing, no
    /// allocation per flash. Overrides the base color property (URP <c>_BaseColor</c>, or legacy
    /// <c>_Color</c>) toward <see cref="FlashColor"/> and fades back, then clears the block.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MaterialFlash : JuiceBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Flash")]
        [Tooltip("Renderers to flash. Empty = all Renderers on this object and children (found on Awake).")]
        public Renderer[] Renderers;
        public Color FlashColor = Color.white;
        [Min(0.01f)] public float Duration = 0.12f;
        [Tooltip("Unscaled time so the flash still animates during a HitStop freeze.")]
        public bool Unscaled = true;

        MaterialPropertyBlock _mpb;
        int[] _propIds;
        Color[] _baseColors;
        float _t = -1f;

        protected override bool DefaultOnSiblingDamage => true;

        void Awake()
        {
            if (Renderers == null || Renderers.Length == 0)
                Renderers = GetComponentsInChildren<Renderer>(true);
            _mpb = new MaterialPropertyBlock();
            _propIds = new int[Renderers.Length];
            _baseColors = new Color[Renderers.Length];
            for (int i = 0; i < Renderers.Length; i++)
            {
                _propIds[i] = 0;
                _baseColors[i] = Color.white;
                var mat = Renderers[i] != null ? Renderers[i].sharedMaterial : null;
                if (mat == null) continue;
                if (mat.HasProperty(BaseColorId)) { _propIds[i] = BaseColorId; _baseColors[i] = mat.GetColor(BaseColorId); }
                else if (mat.HasProperty(ColorId)) { _propIds[i] = ColorId; _baseColors[i] = mat.GetColor(ColorId); }
            }
        }

        public override void Play(float strength) => _t = 0f;

        void Update()
        {
            if (_t < 0f) return;
            _t += (Unscaled ? Time.unscaledDeltaTime : Time.deltaTime) / Duration;
            if (_t >= 1f) { Clear(); _t = -1f; return; }

            float blend = 1f - _t;
            for (int i = 0; i < Renderers.Length; i++)
            {
                if (Renderers[i] == null || _propIds[i] == 0) continue;
                _mpb.Clear();
                _mpb.SetColor(_propIds[i], Color.Lerp(_baseColors[i], FlashColor, blend));
                Renderers[i].SetPropertyBlock(_mpb);
            }
        }

        void Clear()
        {
            for (int i = 0; i < Renderers.Length; i++)
                if (Renderers[i] != null) Renderers[i].SetPropertyBlock(null);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_t >= 0f && Renderers != null) Clear();
            _t = -1f;
        }
    }
}
