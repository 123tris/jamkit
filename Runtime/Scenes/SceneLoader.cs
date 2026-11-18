using Metz.JamKit.Inspector;
using UnityEngine;

namespace Metz.JamKit
{
    public sealed class SceneLoader : MonoBehaviour
    {
        [Required] public SceneServiceSO SceneService;
        public SceneRef Scene;

        [Button]
        public void Load() => SceneService.Load(Scene);

        public void Reload() => SceneService.Reload();
    }
}
