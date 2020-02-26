// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test
{
    internal class TestBlobSink : BlobLoggerProvider
    {
        internal ManualIntervalControl IntervalControl { get; } = new ManualIntervalControl();

        public TestBlobSink(Func<string, ICloudAppendBlob> blobReferenceFactory) : base(
            new OptionsWrapperMonitor<AzureBlobLoggerOptions>(new AzureBlobLoggerOptions()
            {
                ApplicationInstanceId = "42",
                ApplicationName = "appname",
                BlobName = "filename",
                IsEnabled = true
            }),
            blobReferenceFactory)
        {
        }

        protected override Task IntervalAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            return IntervalControl.IntervalAsync();
        }
    }
}
