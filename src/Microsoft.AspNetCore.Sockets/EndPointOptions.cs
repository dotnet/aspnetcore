// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Sockets
{
    public class EndPointOptions<TEndPoint> where TEndPoint : EndPoint
    {
        public AuthorizationPolicy Policy { get; set; }
    }
}
