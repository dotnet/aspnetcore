// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public static partial class HttpConnectionExtensions
    {
        public static IDisposable OnReceived(this HttpConnection connection, Func<byte[], Task> callback)
        {
            return connection.OnReceived((data, state) =>
            {
                var currentCallback = (Func<byte[], Task>)state;
                return currentCallback(data);
            }, callback);
        }
    }
}
