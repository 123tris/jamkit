using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Metz.JamKit
{
    [CreateAssetMenu(menuName = "JamKit/Audio/FMOD Music", fileName = "FmodMusic")]
    public sealed class FmodMusic : Music
    {
        public EventReference Event;

        static EventInstance current;
        static FmodMusic playing;

        public override void Play()
        {
            if (playing == this && current.isValid()) return;
            Stop();
            current = RuntimeManager.CreateInstance(Event);
            current.start();
            playing = this;
        }

        public override void Stop()
        {
            if (!current.isValid()) return;
            current.stop(STOP_MODE.ALLOWFADEOUT);
            current.release();
            current.clearHandle();
            playing = null;
        }

        public void SetParameter(string parameter, float value) => current.setParameterByName(parameter, value);
    }
}
