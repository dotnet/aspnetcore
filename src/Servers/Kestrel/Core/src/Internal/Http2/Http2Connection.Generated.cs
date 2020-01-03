// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal partial class Http2Connection
    {
        // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
     
        private static ReadOnlySpan<byte> ClientPrefaceBytes => new byte[24] { (byte)'P', (byte)'R', (byte)'I', (byte)' ', (byte)'*', (byte)' ', (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'2', (byte)'.', (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n', (byte)'S', (byte)'M', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
        private static ReadOnlySpan<byte> AuthorityBytes => new byte[10] { (byte)':', (byte)'a', (byte)'u', (byte)'t', (byte)'h', (byte)'o', (byte)'r', (byte)'i', (byte)'t', (byte)'y' };
        private static ReadOnlySpan<byte> MethodBytes => new byte[7] { (byte)':', (byte)'m', (byte)'e', (byte)'t', (byte)'h', (byte)'o', (byte)'d' };
        private static ReadOnlySpan<byte> PathBytes => new byte[5] { (byte)':', (byte)'p', (byte)'a', (byte)'t', (byte)'h' };
        private static ReadOnlySpan<byte> SchemeBytes => new byte[7] { (byte)':', (byte)'s', (byte)'c', (byte)'h', (byte)'e', (byte)'m', (byte)'e' };
        private static ReadOnlySpan<byte> StatusBytes => new byte[7] { (byte)':', (byte)'s', (byte)'t', (byte)'a', (byte)'t', (byte)'u', (byte)'s' };
        private static ReadOnlySpan<byte> ConnectionBytes => new byte[10] { (byte)'c', (byte)'o', (byte)'n', (byte)'n', (byte)'e', (byte)'c', (byte)'t', (byte)'i', (byte)'o', (byte)'n' };
        private static ReadOnlySpan<byte> TeBytes => new byte[2] { (byte)'t', (byte)'e' };
        private static ReadOnlySpan<byte> TrailersBytes => new byte[8] { (byte)'t', (byte)'r', (byte)'a', (byte)'i', (byte)'l', (byte)'e', (byte)'r', (byte)'s' };
        private static ReadOnlySpan<byte> ConnectBytes => new byte[7] { (byte)'C', (byte)'O', (byte)'N', (byte)'N', (byte)'E', (byte)'C', (byte)'T' };
    }
}
