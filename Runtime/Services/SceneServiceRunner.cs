using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Metz.JamKit
{
    /// <summary>
    /// Scene-side host for <see cref="SceneServiceSO"/>. Drives the async load coroutine
    /// and the fade overlay. Lives on JamKitCore, one per scene — scenes stay clean slates.
    /// </summary>
    public sealed class SceneServiceRunner : ServiceRunner<SceneServiceSO, SceneServiceRunner>
    {
        [Tooltip("Optional fade overlay for load transitions. Loads still work without one — they just cut instead of fading.")]
        public FadeOverlay Fade;

        bool _loading;

        internal Coroutine Load(string scene, float fadeSeconds, Color fadeColor)
        {
            if (_loading) return null;
            return StartCoroutine(LoadRoutine(scene, fadeSeconds, fadeColor));
        }

        internal Coroutine ReloadCurrent(float fadeSeconds, Color fadeColor)
            => Load(SceneManager.GetActiveScene().name, fadeSeconds, fadeColor);

        IEnumerator LoadRoutine(string scene, float fadeSeconds, Color fadeColor)
        {
            _loading = true;
            Service.RaiseStarted();

            if (Fade != null) yield return Fade.FadeTo(fadeColor, fadeSeconds);
            var op = SceneManager.LoadSceneAsync(scene);
            if (op == null)
            {
                Debug.LogError($"[JamKit] LoadSceneAsync returned null for '{scene}'. Is it in Build Settings?");
                _loading = false;
                yield break;
            }
            while (!op.isDone) yield return null;

            if (Fade != null) yield return Fade.FadeOut(fadeSeconds);

            Service.RaiseCompleted();
            _loading = false;
        }
    }
}
