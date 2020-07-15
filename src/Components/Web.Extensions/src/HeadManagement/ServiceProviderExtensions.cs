// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal static class ServiceProviderExtensions
    {
        public static HeadManager? GetHeadManager(this IServiceProvider serviceProvider)
            => serviceProvider.GetServices<CircuitHandler>().OfType<HeadManager>().SingleOrDefault();
    }
}
