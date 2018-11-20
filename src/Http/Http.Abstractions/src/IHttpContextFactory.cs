// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http
{
    public interface IHttpContextFactory
    {
        HttpContext Create(IFeatureCollection featureCollection);
        void Dispose(HttpContext httpContext);
    }
}
