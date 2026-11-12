using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public abstract class Music : ScriptableObject
    {
        [Button, DisableInEditorMode]
        public abstract void Play();

        [Button, DisableInEditorMode]
        public abstract void Stop();
    }
}
