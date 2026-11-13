using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Services/Screen Fade", fileName = "ScreenFade")]
    public sealed class ScreenFadeSO : ServiceSO
    {
        public Color Color = Color.black;
        [Min(0f)] public float Seconds = 0.35f;

        [Required, Tooltip("The document the fade colors. Its element named fade covers the screen.")]
        public VisualTreeAsset Document;

        [Required, Tooltip("Panel the fade draws on. Keep its sort order above every other panel.")]
        public PanelSettings Panel;

        int latest;

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void FadeOut() => FadeOutAsync();

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void FadeIn() => FadeInAsync();

        [Button, DisableInEditorMode, FoldoutGroup("Debug")]
        public void Flash() => FlashAsync();

        public Awaitable FadeOutAsync() => JamKitHost.Run(Fade(1f));

        public Awaitable FadeInAsync() => JamKitHost.Run(Fade(0f));

        public Awaitable FlashAsync() => JamKitHost.Run(FlashRoutine());

        IEnumerator FlashRoutine()
        {
            yield return Fade(1f);
            yield return Fade(0f);
        }

        IEnumerator Fade(float targetAlpha)
        {
            int mine = ++latest;
            var overlay = JamKitHost.Get<FadeOverlay>();
            overlay.Load(Document, Panel);
            float from = overlay.Alpha;
            for (float t = 0f; t < Seconds && mine == latest; t += Time.unscaledDeltaTime)
            {
                overlay.Show(Color, Mathf.Lerp(from, targetAlpha, t / Seconds));
                yield return null;
            }
            if (mine == latest) overlay.Show(Color, targetAlpha);
        }
    }
}
