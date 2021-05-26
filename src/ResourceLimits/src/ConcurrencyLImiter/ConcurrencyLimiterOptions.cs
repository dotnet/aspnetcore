// Will be migrated to dotnet/runtime
// Pending dotnet API review

namespace System.Threading.ResourceLimits
{
    public class ConcurrencyLimiterOptions
    {
        // Maximum number of resource allowed to be leased
        public virtual long ResourceLimit { get; }

        // Behaviour of WaitAsync when not enough resources can be leased
        public virtual ResourceDepletedMode ResourceDepletedMode { get; }

        // Maximum cumulative resource count of queued acquisition requests
        public virtual long MaxQueueLimit { get; }
    }
}
