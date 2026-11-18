using System.Collections.Generic;
using System.Linq;
using Metz.JamKit.Inspector;
using Ripple;
using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class DisplaySettings : MonoBehaviour
    {
        [Required, Tooltip("Persistent. An index into the project's quality levels; below 0 takes the current level.")]
        public IntVariableSO QualityLevel;

        [Required, Tooltip("Persistent. An index into DisplaySettings.Resolutions; below 0 takes the current resolution.")]
        public IntVariableSO ResolutionIndex;

        [Required, Tooltip("Persistent.")]
        public BoolVariableSO Fullscreen;

        [Required, Tooltip("Persistent. Browsers own VSync, so WebGL ignores it.")]
        public BoolVariableSO VSync;

        static Resolution[] resolutions;
        static bool applied;

        public static IReadOnlyList<Resolution> Resolutions
            => resolutions ??= Screen.resolutions
                .GroupBy(resolution => (resolution.width, resolution.height))
                .Select(sizes => sizes.OrderByDescending(resolution => resolution.refreshRateRatio.value).First())
                .ToArray();

        static bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StartSession() => applied = false;

        void Awake()
        {
            if (QualityLevel.CurrentValue < 0) QualityLevel.CurrentValue = QualitySettings.GetQualityLevel();
            if (ResolutionIndex.CurrentValue < 0) ResolutionIndex.CurrentValue = CurrentResolutionIndex();
        }

        void OnEnable()
        {
            QualityLevel.AddListener(ApplyQuality);
            ResolutionIndex.AddListener(OnResolutionChanged);
            Fullscreen.AddListener(OnFullscreenChanged);
            VSync.AddListener(ApplyVSync);

            if (applied) return;
            applied = true;
            ApplyQuality(QualityLevel.CurrentValue);
            ApplyScreen();
            ApplyVSync(VSync.CurrentValue);
        }

        void OnDisable()
        {
            QualityLevel.RemoveListener(ApplyQuality);
            ResolutionIndex.RemoveListener(OnResolutionChanged);
            Fullscreen.RemoveListener(OnFullscreenChanged);
            VSync.RemoveListener(ApplyVSync);
        }

        void ApplyQuality(int level)
        {
            if (level >= 0) QualitySettings.SetQualityLevel(Mathf.Min(level, QualitySettings.names.Length - 1), true);
        }

        void ApplyVSync(bool on)
        {
            if (!IsWebGL) QualitySettings.vSyncCount = on ? 1 : 0;
        }

        void OnResolutionChanged(int index) => ApplyScreen();

        void OnFullscreenChanged(bool fullscreen) => ApplyScreen();

        void ApplyScreen()
        {
            if (IsWebGL || Resolutions.Count == 0 || ResolutionIndex.CurrentValue < 0) return;
            Resolution resolution = Resolutions[Mathf.Min(ResolutionIndex.CurrentValue, Resolutions.Count - 1)];
            FullScreenMode mode = Fullscreen.CurrentValue ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(resolution.width, resolution.height, mode, resolution.refreshRateRatio);
        }

        static int CurrentResolutionIndex()
        {
            Resolution current = Screen.currentResolution;
            for (int i = 0; i < Resolutions.Count; i++)
                if (Resolutions[i].width == current.width && Resolutions[i].height == current.height)
                    return i;
            return Resolutions.Count - 1;
        }
    }
}
