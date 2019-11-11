// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal interface IHttp3Stream : IThreadPoolWorkItem
    {
        void Abort(ConnectionAbortedException ex); // TODO stream aborted exception?
        Task ProcessRequestAsync<TContext>(IHttpApplication<TContext> application);
    }
}
