using Metz.JamKit.Inspector;
using UnityEngine;

namespace Metz.JamKit
{
    public abstract class Music : ScriptableObject
    {
        [Button]
        public abstract void Play();

        [Button]
        public abstract void Stop();
    }
}
