// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost
{
    internal class RequestLifetimeFeature : IHttpRequestLifetimeFeature
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public RequestLifetimeFeature()
        {
            RequestAborted = _cancellationTokenSource.Token;
        }

        public CancellationToken RequestAborted { get; set; }

        public void Abort() => _cancellationTokenSource.Cancel();
    }
}
