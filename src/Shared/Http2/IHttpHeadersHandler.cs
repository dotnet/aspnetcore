// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Don't ever change this unless we are explicitly trying to remove IHttpHeadersHandler as public API.
#if KESTREL
using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
#else
namespace System.Net.Http
#endif
{
#if KESTREL
    public
#else
    internal
#endif
    interface IHttpHeadersHandler
    {
        void OnStaticIndexedHeader(int index);
        void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value);
        void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value);
        void OnHeadersComplete(bool endStream);
    }
}
