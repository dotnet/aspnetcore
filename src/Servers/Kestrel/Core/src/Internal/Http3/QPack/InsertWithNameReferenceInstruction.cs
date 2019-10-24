// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    public struct InsertWithNameReferenceInstruction
    {
        public InsertWithNameReferenceInstruction(Span<byte> buffer)
        {
            // first bit will be 1 here
            // parse second bit, if s=1, static, else dynamic

            // Next a 6 bit prefix integer for static table index or relative index
            // afterwards, it's a string literal (same as hpack).
        }
    }
}
