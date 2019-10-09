// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal interface IHttp2StreamLifetimeHandler
    {
        void OnStreamCompleted(Http2Stream stream);
        void DecrementActiveClientStreamCount();
    }
}
