// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost
{
    internal class RequestLifetimeFeature : IHttpRequestLifetimeFeature
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Action<Exception> _abort;

        public RequestLifetimeFeature(Action<Exception> abort)
        {
            RequestAborted = _cancellationTokenSource.Token;
            _abort = abort;
        }

        public CancellationToken RequestAborted { get; set; }

        internal void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        void IHttpRequestLifetimeFeature.Abort()
        {
            _abort(new Exception("The application aborted the request."));
            _cancellationTokenSource.Cancel();
        }
    }
}
