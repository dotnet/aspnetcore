// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection
{
    internal class RC1ForwardingActivator: SimpleActivator
    {
        private const string From = "Microsoft.AspNet.DataProtection";
        private const string To = "Microsoft.AspNetCore.DataProtection";
        private readonly ILogger _logger;

        public RC1ForwardingActivator(IServiceProvider services) : this(services, DataProtectionProviderFactory.GetDefaultLoggerFactory())
        {
        }

        public RC1ForwardingActivator(IServiceProvider services, ILoggerFactory loggerFactory) : base(services)
        {
            _logger = loggerFactory.CreateLogger(typeof(RC1ForwardingActivator));
        }

        public override object CreateInstance(Type expectedBaseType, string implementationTypeName)
        {
            if (implementationTypeName.Contains(From))
            {
                var forwardedImplementationTypeName = implementationTypeName.Replace(From, To);
                var type = Type.GetType(forwardedImplementationTypeName, false);
                if (type != null)
                {
                    _logger.LogDebug("Forwarded activator type request from {FromType} to {ToType}",
                        implementationTypeName,
                        forwardedImplementationTypeName);

                    implementationTypeName = forwardedImplementationTypeName;
                }
            }
            return base.CreateInstance(expectedBaseType, implementationTypeName);
        }
    }
}