// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public interface IFrameControl
    {
        void ProduceContinue();
        void Write(ArraySegment<byte> data, Action<Exception, object> callback, object state);
    }
}