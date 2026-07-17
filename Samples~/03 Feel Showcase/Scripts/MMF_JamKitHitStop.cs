using MoreMountains.Feedbacks;
using UnityEngine;

namespace Metz.JamKit.Samples
{
    /// <summary>
    /// Feel feedback that routes freeze-frames through JamKit's <see cref="HitStop"/> (and
    /// therefore the TimeServiceSO push/pop stack) instead of stomping Time.timeScale — the only
    /// legal way to freeze time in a JamKit project (PILLARS.md). Feel's own time feedbacks fight
    /// the pause menu; this one composes with it. Drop it into any MMF_Player stack.
    /// </summary>
    [AddComponentMenu("")]
    [System.Serializable]
    [FeedbackPath("Time/JamKit Hit Stop")]
    public class MMF_JamKitHitStop : MMF_Feedback
    {
        [Tooltip("Scene HitStop this feedback plays — its TimeService is auto-assigned at sample setup.")]
        public HitStop HitStop;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (!Active || HitStop == null) return;
            HitStop.Play(feedbacksIntensity);
        }
    }
}
