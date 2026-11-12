using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class SpawnBurst : MonoBehaviour
    {
        [Required] public GameObject Prefab;
        public IntReference Count = new(3);
        public FloatReference Scatter = new(0.4f);

        [Tooltip("Scatter on the XY plane instead of in a sphere.")]
        public bool Is2D;

        public bool RandomRotation = true;

        [Tooltip("Outward speed given to spawned rigidbodies. 0 leaves them still.")]
        public FloatReference LaunchSpeed = new(0f);

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Burst() => Burst(1f);

        public void Burst(float countMultiplier)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(Count.Value * countMultiplier));
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Is2D ? Random.insideUnitCircle : Random.insideUnitSphere;
                GameObject instance = Instantiate(Prefab, transform.position + direction * Scatter.Value, Rotation());
                new Body(instance).Velocity = direction.normalized * LaunchSpeed.Value;
            }
        }

        Quaternion Rotation()
        {
            if (!RandomRotation) return transform.rotation;
            return Is2D ? Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)) : Random.rotation;
        }
    }
}
