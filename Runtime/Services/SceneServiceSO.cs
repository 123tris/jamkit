using System.Collections;
using Ripple;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Services/Scene Service", fileName = "SceneService")]
    public sealed class SceneServiceSO : ServiceSO
    {
        [Tooltip("Loads cut instead of fading when empty.")]
        public ScreenFadeSO Fade;

        public Signal OnLoadStarted = new();
        public Signal OnLoadCompleted = new();

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug")]
        public bool IsLoading { get; private set; }

        public override void ResetState() => IsLoading = false;

        public void Load(string sceneName) => LoadAsync(sceneName);

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Reload() => ReloadAsync();

        public Awaitable ReloadAsync() => LoadAsync(SceneManager.GetActiveScene().name);

        public Awaitable LoadAsync(string sceneName) => JamKitHost.Run(LoadRoutine(sceneName));

        IEnumerator LoadRoutine(string sceneName)
        {
            if (IsLoading) yield break;
            IsLoading = true;
            OnLoadStarted.Invoke();

            if (Fade != null) yield return Fade.FadeOutAsync();
            yield return SceneManager.LoadSceneAsync(sceneName);
            if (Fade != null) yield return Fade.FadeInAsync();

            IsLoading = false;
            OnLoadCompleted.Invoke();
        }
    }
}
