// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Hosting.Builder
{
    public interface IHttpContextFactory
    {
        HttpContext CreateHttpContext(IFeatureCollection featureCollection);
    }
}