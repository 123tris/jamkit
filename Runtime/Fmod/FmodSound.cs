using FMODUnity;
using UnityEngine;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Audio/FMOD Sound", fileName = "FmodSound")]
    public sealed class FmodSound : Sound
    {
        public EventReference Event;

        public override void Play() => RuntimeManager.PlayOneShot(Event);

        public override void PlayAt(Vector3 position) => RuntimeManager.PlayOneShot(Event, position);
    }
}
