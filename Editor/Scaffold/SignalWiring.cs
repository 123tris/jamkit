using Ripple;
using UnityEngine.Events;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// The UnityEvent behind a Signal, for wiring persistent calls from scaffolding code.
    /// A Signal set to another response (UltEvents, a project's own) is switched to a UnityEvent response.
    /// </summary>
    internal static class SignalWiring
    {
        public static UnityEvent Calls(this Signal signal)
        {
            if (signal.Response is not UnityEventResponse response)
                signal.Response = response = new UnityEventResponse();
            return response.Event;
        }

        public static UnityEvent<T> Calls<T>(this Signal<T> signal)
        {
            if (signal.Response is not UnityEventResponse<T> response)
                signal.Response = response = new UnityEventResponse<T>();
            return response.Event;
        }
    }
}
