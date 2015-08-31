// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Builder
{
    public interface IApplicationBuilder
    {
        IServiceProvider ApplicationServices { get; set; }

        IFeatureCollection ServerFeatures { get; }

        IDictionary<string, object> Properties { get; }

        IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);

        IApplicationBuilder New();

        RequestDelegate Build();
    }
}
