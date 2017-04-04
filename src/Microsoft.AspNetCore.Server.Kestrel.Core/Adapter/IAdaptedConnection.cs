// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter
{
    public interface IAdaptedConnection
    {
        Stream ConnectionStream { get; }

        void PrepareRequest(IFeatureCollection requestFeatures);
    }
}
