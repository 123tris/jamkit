using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Metz.JamKit
{
    /// <summary>
    /// Scene-side host for <see cref="SceneServiceSO"/>. Drives the async load coroutine
    /// and the fade overlay. Drop one into your bootstrap scene with DontDestroyOnLoad,
    /// or one per scene if you don't need a persistent root.
    /// </summary>
    public sealed class SceneServiceRunner : MonoBehaviour
    {
        public SceneServiceSO Service;
        public FadeOverlay Fade;

        bool _loading;

        void OnEnable() { if (Service != null) Service.RegisterRunner(this); }
        void OnDisable() { if (Service != null) Service.UnregisterRunner(this); }

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
