// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Http.Features
{
    internal class RequestLifetimeFeature : IHttpRequestLifetimeFeature
    {
        private readonly CancellationTokenSource _abortableCancellationTokenSource = new CancellationTokenSource();
        private CancellationTokenSource _linkedCancellationTokenSource;

        public CancellationToken RequestAborted
        {
            get => _linkedCancellationTokenSource?.Token ?? _abortableCancellationTokenSource.Token;
            set => _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    token1: _abortableCancellationTokenSource.Token,
                    token2: value);
        }

        public void Abort() => _abortableCancellationTokenSource.Cancel();
    }
}
