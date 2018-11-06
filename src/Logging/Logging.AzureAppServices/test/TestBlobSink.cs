using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.AzureAppServices.Internal;
using Microsoft.Extensions.Options;

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