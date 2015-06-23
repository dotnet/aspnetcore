// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    ///   Operations performed for buffered socket output
    /// </summary>
    public interface ISocketOutput
    {
        void Write(ArraySegment<byte> buffer, Action<Exception, object> callback, object state);
    }
}