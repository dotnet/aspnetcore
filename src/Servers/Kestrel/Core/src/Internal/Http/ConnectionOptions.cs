// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

[Flags]
internal enum ConnectionOptions
{
    None = 0,
    Close = 1,
    KeepAlive = 2,
    Upgrade = 4
}
