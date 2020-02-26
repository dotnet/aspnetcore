// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.DependencyInjection
{
    internal class DefaultHttpClientBuilder : IHttpClientBuilder
    {
        public DefaultHttpClientBuilder(IServiceCollection services, string name)
        {
            Services = services;
            Name = name; 
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }
}
