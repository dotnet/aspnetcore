// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection
{
    internal class TypeForwardingActivator : SimpleActivator
    {
        private const string OldNamespace = "Microsoft.AspNet.DataProtection";
        private const string CurrentNamespace = "Microsoft.AspNetCore.DataProtection";
        private readonly ILogger _logger;
        private static readonly Regex _versionPattern = new Regex(@",\s?Version=[0-9]+(\.[0-9]+){0,3}", RegexOptions.Compiled, TimeSpan.FromSeconds(2));

        public TypeForwardingActivator(IServiceProvider services)
            : this(services, NullLoggerFactory.Instance)
        {
        }

        public TypeForwardingActivator(IServiceProvider services, ILoggerFactory loggerFactory)
            : base(services)
        {
            _logger = loggerFactory.CreateLogger(typeof(TypeForwardingActivator));
        }

        public override object CreateInstance(Type expectedBaseType, string originalTypeName)
            => CreateInstance(expectedBaseType, originalTypeName, out var _);

        // for testing
        internal object CreateInstance(Type expectedBaseType, string originalTypeName, out bool forwarded)
        {
            var forwardedTypeName = originalTypeName;
            var candidate = false;
            if (originalTypeName.Contains(OldNamespace))
            {
                candidate = true;
                forwardedTypeName = originalTypeName.Replace(OldNamespace, CurrentNamespace);
            }

            if (candidate || forwardedTypeName.StartsWith(CurrentNamespace + ".", StringComparison.Ordinal))
            {
                candidate = true;
                forwardedTypeName = RemoveVersionFromAssemblyName(forwardedTypeName);
            }

            if (candidate)
            {
                var type = Type.GetType(forwardedTypeName, false);
                if (type != null)
                {
                    _logger.LogDebug("Forwarded activator type request from {FromType} to {ToType}",
                        originalTypeName,
                        forwardedTypeName);
                    forwarded = true;
                    return base.CreateInstance(expectedBaseType, forwardedTypeName);
                }
            }

            forwarded = false;
            return base.CreateInstance(expectedBaseType, originalTypeName);
        }

        protected string RemoveVersionFromAssemblyName(string forwardedTypeName)
            => _versionPattern.Replace(forwardedTypeName, "");
    }
}
