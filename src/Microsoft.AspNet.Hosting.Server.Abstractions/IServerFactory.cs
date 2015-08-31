// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.Framework.Configuration;

namespace Microsoft.AspNet.Hosting.Server
{
    public interface IServerFactory
    {
        IFeatureCollection Initialize(IConfiguration configuration);
        IDisposable Start(IFeatureCollection serverFeatures, Func<IFeatureCollection, Task> application);
    }
}
