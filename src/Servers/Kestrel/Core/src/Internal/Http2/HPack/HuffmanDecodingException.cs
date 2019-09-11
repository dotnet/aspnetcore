// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    internal sealed class HuffmanDecodingException : Exception
    {
        public HuffmanDecodingException()
        {
        }

        public HuffmanDecodingException(string message)
            : base(message)
        {
        }
        
        public HuffmanDecodingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
