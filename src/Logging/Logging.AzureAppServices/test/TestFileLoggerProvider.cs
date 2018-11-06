// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.AzureAppServices.Internal;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test
{
    internal class TestFileLoggerProvider : FileLoggerProvider
    {
        internal ManualIntervalControl IntervalControl { get; } = new ManualIntervalControl();

        public TestFileLoggerProvider(
            string path,
            string fileName = "LogFile.",
            int maxFileSize = 32_000,
            int maxRetainedFiles = 100)
            : base(new OptionsWrapperMonitor<AzureFileLoggerOptions>(new AzureFileLoggerOptions()
        {
            LogDirectory = path,
            FileName = fileName,
            FileSizeLimit = maxFileSize,
            RetainedFileCountLimit = maxRetainedFiles,
            IsEnabled = true
        }))
        {
        }

        protected override Task IntervalAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            return IntervalControl.IntervalAsync();
        }
    }
}