// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

namespace System.Net.Http
{
    internal partial class Http3RawFrame
    {
        public long Length { get; set; }

        public Http3FrameType Type { get; internal set; }

        public string FormattedType => Http3Formatting.ToFormattedType(Type);

        public override string ToString()
        {
            return $"{FormattedType} Length: {Length}";
        }
    }
}
