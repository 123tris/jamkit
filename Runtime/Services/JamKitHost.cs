using System.Collections;
using UnityEngine;

namespace Metz.JamKit
{
    internal sealed class JamKitHost : MonoBehaviour
    {
        static JamKitHost instance;

        static JamKitHost Instance
        {
            get
            {
                if (instance != null) return instance;
                var host = new GameObject("[JamKit]");
                if (Application.isPlaying) DontDestroyOnLoad(host);
                instance = host.AddComponent<JamKitHost>();
                return instance;
            }
        }

        public static T Get<T>() where T : Component
            => Instance.TryGetComponent(out T component) ? component : Instance.gameObject.AddComponent<T>();

        public static Awaitable Run(IEnumerator routine)
        {
            var completion = new AwaitableCompletionSource();
            Instance.StartCoroutine(Complete(routine, completion));
            return completion.Awaitable;
        }

        static IEnumerator Complete(IEnumerator routine, AwaitableCompletionSource completion)
        {
            yield return routine;
            completion.SetResult();
        }
    }
}
