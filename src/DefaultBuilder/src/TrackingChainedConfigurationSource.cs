// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Builder
{
    internal sealed class TrackingChainedConfigurationSource : IConfigurationSource
    {
        private readonly ChainedConfigurationSource _chainedConfigurationSource = new();

        public TrackingChainedConfigurationSource(ConfigurationManager configManager)
        {
            _chainedConfigurationSource.Configuration = configManager;
        }

        public IConfigurationProvider? BuiltProvider { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            BuiltProvider = _chainedConfigurationSource.Build(builder);
            return BuiltProvider;
        }
    }
}
