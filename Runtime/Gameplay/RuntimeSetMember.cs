using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Registers this GameObject in a Ripple runtime set (<see cref="GameObjectListSO"/>) while
    /// enabled. The data-driven alternative to tags and scene scans: players/targets/allies join
    /// the set on enable, leave on disable, and systems (e.g. <see cref="ChaseMover"/>) read the
    /// set asset instead of searching the scene.
    /// </summary>
    public sealed class RuntimeSetMember : MonoBehaviour
    {
        [Required, Tooltip("The Ripple GameObjectListSO this object joins while active.")]
        public GameObjectListSO Set;

        void OnEnable()
        {
            if (Set != null && !Set.Contains(gameObject)) Set.Add(gameObject);
        }

        void OnDisable()
        {
            if (Set != null) Set.Remove(gameObject);
        }
    }
}
