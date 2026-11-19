using System.Collections;
using Metz.JamKit.Inspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Services/Screen Fade", fileName = "ScreenFade")]
    public sealed class ScreenFadeSO : ServiceSO
    {
        public Color Color = Color.black;
        [Min(0f)] public float Seconds = 0.35f;

        [Range(0f, 1f), Tooltip("How opaque Flash gets at its peak. A hit flash is usually well below 1.")]
        public float FlashAlpha = 1f;

        [Required, Tooltip("The document the fade colors. Its element named fade covers the screen.")]
        public VisualTreeAsset Document;

        [Required, Tooltip("Panel the fade draws on. Keep its sort order above every other panel.")]
        public PanelSettings Panel;

        int latest;
        FadeOverlay overlay;

        FadeOverlay Overlay
        {
            get
            {
                if (overlay == null) overlay = FadeOverlay.Create(name);
                return overlay;
            }
        }

        [Button]
        public void FadeOut() => FadeOutAsync();

        [Button]
        public void FadeIn() => FadeInAsync();

        [Button]
        public void Flash() => FlashAsync();

        public Awaitable FadeOutAsync() => JamKitHost.Run(Fade(1f));

        public Awaitable FadeInAsync() => JamKitHost.Run(Fade(0f));

        public Awaitable FlashAsync() => JamKitHost.Run(FlashRoutine());

        IEnumerator FlashRoutine()
        {
            yield return Fade(FlashAlpha);
            yield return Fade(0f);
        }

        IEnumerator Fade(float targetAlpha)
        {
            int mine = ++latest;
            FadeOverlay layer = Overlay;
            layer.Load(Document, Panel);
            float from = layer.Alpha;
            for (float t = 0f; t < Seconds && mine == latest; t += Time.unscaledDeltaTime)
            {
                layer.Show(Color, Mathf.Lerp(from, targetAlpha, t / Seconds));
                yield return null;
            }
            if (mine == latest) layer.Show(Color, targetAlpha);
        }
    }
}
