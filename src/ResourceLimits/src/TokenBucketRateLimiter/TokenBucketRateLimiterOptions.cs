// Will be migrated to dotnet/runtime
// Pending dotnet API review

namespace System.Runtime.RateLimits
{
    public class TokenBucketRateLimiterOptions
    {
        // Maximum number of permits allowed to be leased
        public virtual int PermitLimit { get; set; }

        // Behaviour of WaitAsync when not enough resources can be leased
        public virtual PermitsExhaustedMode PermitsExhaustedMode { get; set; }

        // Maximum cumulative permit count of queued acquisition requests
        public virtual int QueueLimit { get; set; }

        // Specifies the period between replenishments
        public virtual TimeSpan ReplenishmentPeriod { get; set; }

        // Specifies how many tokens to restore each replenishment
        public virtual int TokensPerPeriod { get; set; }
    }
}
