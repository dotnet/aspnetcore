// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal interface IRequestProcessor
{
    Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application) where TContext : notnull;
    void StopProcessingNextRequest();
    void HandleRequestHeadersTimeout();
    void HandleReadDataRateTimeout();
    void OnInputOrOutputCompleted();
    void Tick(DateTimeOffset now);
    void Abort(ConnectionAbortedException ex);
}
