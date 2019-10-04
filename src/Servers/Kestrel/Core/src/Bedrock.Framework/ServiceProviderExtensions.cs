using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bedrock.Framework
{
    internal static class ServiceProviderExtensions
    {
        internal static ILoggerFactory GetLoggerFactory(this IServiceProvider serviceProvider)
        {
            return (ILoggerFactory)serviceProvider?.GetService(typeof(ILoggerFactory)) ?? NullLoggerFactory.Instance;
        }
    }
}
