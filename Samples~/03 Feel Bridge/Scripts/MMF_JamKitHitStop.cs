using MoreMountains.Feedbacks;
using UnityEngine;

namespace Metz.JamKit.Samples
{
    [AddComponentMenu("")]
    [System.Serializable]
    [FeedbackPath("Time/JamKit Hit Stop")]
    public class MMF_JamKitHitStop : MMF_Feedback
    {
        [Tooltip("The HitStop this feedback plays. The player starters have one.")]
        public HitStop HitStop;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (!Active || HitStop == null) return;
            HitStop.Play(feedbacksIntensity);
        }
    }
}
