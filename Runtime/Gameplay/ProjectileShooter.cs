using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metz.JamKit
{
    public sealed class ProjectileShooter : MonoBehaviour
    {
        [Required] public GameObject ProjectilePrefab;

        [Required, Tooltip("Spawn point. 3D fires along its forward axis, 2D along its right axis.")]
        public Transform Muzzle;

        public FloatReference Speed = new(12f);
        public FloatReference FireInterval = new(0.2f);

        [Tooltip("Fire along the muzzle right axis instead of forward.")]
        public bool Is2D;

        [Tooltip("Fires while held. Leave empty to fire automatically.")]
        public InputActionReference Attack;

        [FoldoutGroup("Events")] public Signal OnFired = new();

        Cooldown cooldown;

        void Reset() => Muzzle = transform;

        void OnEnable()
        {
            if (Attack != null) Attack.action.Enable();
        }

        void Update()
        {
            if (Attack != null && !Attack.action.IsPressed()) return;
            if (cooldown.TryUse(FireInterval.Value)) Fire();
        }

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public GameObject Fire()
        {
            GameObject projectile = Instantiate(ProjectilePrefab, Muzzle.position, Muzzle.rotation);
            new Body(projectile).Velocity = (Is2D ? Muzzle.right : Muzzle.forward) * Speed.Value;
            OnFired.Invoke();
            return projectile;
        }
    }
}
