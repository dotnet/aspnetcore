// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Hosting.Builder
{
    public interface IApplicationBuilderFactory
    {
        IApplicationBuilder CreateBuilder(IFeatureCollection serverFeatures);
    }
}