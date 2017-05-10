// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.Service
{
    internal class IdentityServiceBuilder<TUser,TApplication> : IIdentityServiceBuilder
    {
        public IdentityServiceBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        public Type ApplicationType => typeof(TApplication);
        public Type UserType => typeof(TUser);
    }
}
