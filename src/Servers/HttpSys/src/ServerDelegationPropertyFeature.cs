using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal class ServerDelegationPropertyFeature : IServerDelegationFeature
    {
        private readonly ILogger _logger;
        private readonly RequestQueue _queue;

        public ServerDelegationPropertyFeature(RequestQueue queue, ILogger logger)
        {
            _queue = queue;
            _logger = logger;
        }

        public DelegationRule CreateDelegationRule(string queueName, string uri)
        {
            var rule = new DelegationRule(_queue.UrlGroup, queueName, uri, _logger);
            _queue.UrlGroup.SetDelegationProperty(rule.Queue);
            return rule;
        }
    }
}
