// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public interface IHttpRequestLineHandler
    {
        void OnStartLine(HttpMethod method, HttpVersion version, ReadOnlySpan<byte> target, ReadOnlySpan<byte> path, ReadOnlySpan<byte> query, ReadOnlySpan<byte> customMethod, bool pathEncoded);
    }
}