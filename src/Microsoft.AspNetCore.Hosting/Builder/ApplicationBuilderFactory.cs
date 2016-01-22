// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Builder.Internal;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Hosting.Builder
{
    public class ApplicationBuilderFactory : IApplicationBuilderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ApplicationBuilderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IApplicationBuilder CreateBuilder(IFeatureCollection serverFeatures)
        {
            return new ApplicationBuilder(_serviceProvider, serverFeatures);
        }
    }
}
