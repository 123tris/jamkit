using Ripple;

namespace Metz.JamKit
{
    public sealed class PauseCoordinator
    {
        readonly TimeServiceSO _time;
        readonly VoidEventSO _onOpened, _onClosed;
        bool _paused;

        public PauseCoordinator(TimeServiceSO time, VoidEventSO onOpened, VoidEventSO onClosed)
        {
            _time = time;
            _onOpened = onOpened;
            _onClosed = onClosed;
        }

        public void Pause()
        {
            if (_paused || _time == null) return;
            _time.Pause();
            _onOpened?.Invoke();
            _paused = true;
        }

        public void Resume()
        {
            if (!_paused || _time == null) return;
            _time.Resume();
            _onClosed?.Invoke();
            _paused = false;
        }
    }
}
