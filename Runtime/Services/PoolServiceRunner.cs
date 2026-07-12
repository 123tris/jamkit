namespace Metz.JamKit
{
    /// <summary>
    /// Scene-side host for <see cref="PoolServiceSO"/>: idle pooled instances parent under this
    /// transform so the hierarchy stays tidy. Registration (and the per-scene pool reset that
    /// keeps Domain-Reload-off sessions clean) comes entirely from the base class.
    /// </summary>
    public sealed class PoolServiceRunner : ServiceRunner<PoolServiceSO, PoolServiceRunner>
    {
    }
}
