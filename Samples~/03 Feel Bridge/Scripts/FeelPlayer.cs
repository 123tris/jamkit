using MoreMountains.Feedbacks;
using UnityEngine;

namespace Metz.JamKit.Samples
{
    public sealed class FeelPlayer : MonoBehaviour
    {
        public MMF_Player Player;

        public void Play(float intensity)
        {
            if (Player != null) Player.PlayFeedbacks(transform.position, intensity);
        }
    }
}
