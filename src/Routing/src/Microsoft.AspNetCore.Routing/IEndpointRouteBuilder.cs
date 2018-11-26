// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Routing
{
    public interface IEndpointRouteBuilder
    {
        IApplicationBuilder CreateApplicationBuilder();

        IServiceProvider ServiceProvider { get; }

        ICollection<EndpointDataSource> DataSources { get; }
    }
}