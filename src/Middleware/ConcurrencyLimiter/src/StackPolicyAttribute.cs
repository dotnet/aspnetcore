using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    /// <summary>
    /// 
    /// </summary>
    public class StackPolicyAttribute : Attribute, IQueuePolicy
    {
        private readonly StackPolicy _stackPolicy;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxConcurrentRequests">
        /// Maximum number of concurrent requests. Any extras will be queued on the server. 
        /// This option is highly application dependant, and must be configured by the application.
        /// </param>
        /// <param name="requestQueueLimit">
        ///Maximum number of queued requests before the server starts rejecting connections with '503 Service Unavailible'.
        /// This option is highly application dependant, and must be configured by the application.
        /// </param>
        public StackPolicyAttribute(int maxConcurrentRequests, int requestQueueLimit)
        {
            _stackPolicy = new StackPolicy(Options.Create(new QueuePolicyOptions()
            {
                MaxConcurrentRequests = maxConcurrentRequests,
                RequestQueueLimit = requestQueueLimit
            }));
        }

        public void OnExit()
            => _stackPolicy.OnExit();

        public ValueTask<bool> TryEnterAsync()
            => _stackPolicy.TryEnterAsync();
    }
}
