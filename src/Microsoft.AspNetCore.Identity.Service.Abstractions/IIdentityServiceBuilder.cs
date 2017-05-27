// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.AspNetCore.Identity.Service
{
    public interface IIdentityServiceBuilder
    {
        IServiceCollection Services { get; }
        Type ApplicationType { get; }
        Type UserType { get; }
        Type RoleType { get; }
    }
}
