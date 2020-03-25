// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;

namespace Microsoft.AspNetCore.Connections.Features
{
    public interface IConnectionEndPointFeature
    {
        EndPoint LocalEndPoint { get; set; }
        EndPoint RemoteEndPoint { get; set; }
    }
}
