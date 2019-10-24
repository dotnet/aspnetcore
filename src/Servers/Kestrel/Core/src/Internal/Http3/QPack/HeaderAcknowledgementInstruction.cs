// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    public struct HeaderAcknowledgementInstruction
    {
        public HeaderAcknowledgementInstruction(Span<byte> buffer)
        {
            // First byte is a 1, then stream id.
        }
    }
}
