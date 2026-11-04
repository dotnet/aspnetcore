// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal interface IHttp2StreamLifetimeHandler
{
    void OnStreamCompleted(Http2Stream stream);
    void DecrementActiveClientStreamCount();
}
