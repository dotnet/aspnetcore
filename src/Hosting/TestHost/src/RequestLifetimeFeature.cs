// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Http.Features
{
    internal class RequestLifetimeFeature : IHttpRequestLifetimeFeature
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public CancellationToken RequestAborted
        {
            get => _cancellationTokenSource.Token;
            set => throw new NotSupportedException();
        }

        public void Abort() => _cancellationTokenSource.Cancel();
    }
}
