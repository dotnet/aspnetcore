// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Connections.Internal;

internal enum HttpConnectionStatus
{
    Inactive,
    Active,
    Disposed
}
