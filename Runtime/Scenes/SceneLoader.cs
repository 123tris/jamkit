using Sirenix.OdinInspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class SceneLoader : MonoBehaviour
    {
        [Required] public SceneServiceSO SceneService;
        public SceneRef Scene;

        [Button, DisableInEditorMode]
        public void Load() => SceneService.Load(Scene);

        public void Reload() => SceneService.Reload();
    }
}
