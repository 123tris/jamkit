using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public abstract class Sound : ScriptableObject
    {
        [Button, DisableInEditorMode]
        public abstract void Play();

        public abstract void PlayAt(Vector3 position);

        public void PlayAt(Transform target) => PlayAt(target.position);
    }
}
