using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class HitStop : MonoBehaviour
    {
        [Required] public TimeServiceSO TimeService;

        [Tooltip("Freeze length at strength 1.")]
        public FloatReference Duration = new(0.05f);

        [Min(0f)] public float MaxDuration = 0.25f;
        [Range(0f, 1f)] public float TimeScale;

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Play() => Play(1f);

        public void Play(float strength)
            => TimeService.FreezeAsync(Mathf.Min(Duration.Value * strength, MaxDuration), TimeScale);
    }
}
