// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Internal
{
    public class MvcBuilder : IMvcBuilder
    {
        public MvcBuilder([NotNull] IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}