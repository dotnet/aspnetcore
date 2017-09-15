// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Stream : IHttp2StreamIdFeature
    {
        int IHttp2StreamIdFeature.StreamId => _context.StreamId;
    }
}
