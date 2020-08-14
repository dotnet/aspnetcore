using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class DelegationRule
    {
        private readonly ILogger _logger;
        public string QueueName { get; }
        public string UrlPrefix { get; }
        internal RequestQueue Queue { get; }

        internal DelegationRule(string queueName, string urlPrefix, ILogger logger)
        {
            _logger = logger;
            QueueName = queueName;
            UrlPrefix = urlPrefix;
            Queue = new RequestQueue(null, queueName, UrlPrefix, _logger, receiver: true);
        }
    }
}
