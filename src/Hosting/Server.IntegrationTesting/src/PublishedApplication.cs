// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class PublishedApplication: IDisposable
    {
        private readonly ILogger _logger;

        public string Path { get; }

        public PublishedApplication(string path, ILogger logger)
        {
            _logger = logger;
            Path = path;
        }

        public void Dispose()
        {
            RetryHelper.RetryOperation(
                () => Directory.Delete(Path, true),
                e => _logger.LogWarning($"Failed to delete directory : {e.Message}"),
                retryCount: 3,
                retryDelayMilliseconds: 100);
        }
    }
}