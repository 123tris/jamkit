using Metz.JamKit.Inspector;
using UnityEngine;

namespace Metz.JamKit
{
    public abstract class Sound : ScriptableObject
    {
        [Button]
        public abstract void Play();

        public abstract void PlayAt(Vector3 position);

        public void PlayAt(Transform target) => PlayAt(target.position);
    }
}
