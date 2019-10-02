// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal class GenericWebHostApplicationLifetime : IApplicationLifetime
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        public GenericWebHostApplicationLifetime(IHostApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
        }

        public CancellationToken ApplicationStarted => _applicationLifetime.ApplicationStarted;

        public CancellationToken ApplicationStopping => _applicationLifetime.ApplicationStopping;

        public CancellationToken ApplicationStopped => _applicationLifetime.ApplicationStopped;

        public void StopApplication() => _applicationLifetime.StopApplication();
    }
}
