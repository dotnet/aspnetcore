// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal sealed class Http1Connection<TContext> : Http1Connection, IHostContextContainer<TContext> where TContext : notnull
{
    public Http1Connection(HttpConnectionContext context) : base(context) { }

    TContext? IHostContextContainer<TContext>.HostContext { get; set; }
}
