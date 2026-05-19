// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal static class TlsAlpn
{
    // Replace with https://github.com/dotnet/runtime/issues/79687
    public static bool IsSupported { get; } = true;
}
