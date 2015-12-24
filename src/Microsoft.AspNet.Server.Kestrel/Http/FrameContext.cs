// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class FrameContext : ConnectionContext
    {
        public FrameContext()
        {
        }

        public FrameContext(ConnectionContext context) : base(context)
        {
        }

        public IFrameControl FrameControl { get; set; }
    }
}