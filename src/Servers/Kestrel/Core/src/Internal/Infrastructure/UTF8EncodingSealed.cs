// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    // Allow for de-virtualization (see https://github.com/dotnet/coreclr/pull/9230)
    internal sealed class UTF8EncodingSealed : UTF8Encoding
    {
        public UTF8EncodingSealed() : base(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true) { }

        public override byte[] GetPreamble() => Array.Empty<byte>();
    }
}
