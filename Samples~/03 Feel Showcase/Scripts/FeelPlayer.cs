using MoreMountains.Feedbacks;
using UnityEngine;

namespace Metz.JamKit.Samples
{
    /// <summary>
    /// Ten-line bridge for value-scaled feel: wire <c>Health.OnDamaged (UltEvent&lt;float&gt;) →
    /// Play(float)</c> and the damage amount becomes MMF_Player intensity, so big hits feel
    /// bigger. (When you don't need scaling, wire <c>MMF_Player.PlayFeedbacks()</c> directly.)
    /// </summary>
    public sealed class FeelPlayer : MonoBehaviour
    {
        public MMF_Player Player;

        public void Play(float intensity)
        {
            if (Player != null) Player.PlayFeedbacks(transform.position, intensity);
        }
    }
}
