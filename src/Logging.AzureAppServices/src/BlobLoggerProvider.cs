// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.AzureAppServices
{
    /// <summary>
    /// The <see cref="ILoggerProvider"/> implementation that stores messages by appending them to Azure Blob in batches.
    /// </summary>
    [ProviderAlias("AzureAppServicesBlob")]
    public class BlobLoggerProvider : BatchingLoggerProvider
    {
        private readonly IOptionsMonitor<AzureBlobLoggerOptions> _options;
        private readonly Func<string, ICloudAppendBlob> _blobReferenceFactory;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Creates a new instance of <see cref="BlobLoggerProvider"/>
        /// </summary>
        /// <param name="options">The options to use when creating a provider.</param>
        [SuppressMessage("ApiDesign", "RS0022:Constructor make noninheritable base class inheritable", Justification = "Required for backwards compatibility")]
        public BlobLoggerProvider(IOptionsMonitor<AzureBlobLoggerOptions> options)
            : this(options, null)
        {
            _blobReferenceFactory = name => new BlobAppendReferenceWrapper(
                options.CurrentValue.ContainerUrl,
                name,
                _httpClient);
        }

        /// <summary>
        /// Creates a new instance of <see cref="BlobLoggerProvider"/>
        /// </summary>
        /// <param name="blobReferenceFactory">The container to store logs to.</param>
        /// <param name="options">Options to be used in creating a logger.</param>
        internal BlobLoggerProvider(
            IOptionsMonitor<AzureBlobLoggerOptions> options,
            Func<string, ICloudAppendBlob> blobReferenceFactory) :
            base(options)
        {
            _options = options;
            _blobReferenceFactory = blobReferenceFactory;
            _httpClient = new HttpClient();
        }

        internal override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken)
        {
            var eventGroups = messages.GroupBy(GetBlobKey);
            foreach (var eventGroup in eventGroups)
            {
                var key = eventGroup.Key;
                var options = _options.CurrentValue;
                string blobName = options.FileNameFormat(new AzureBlobLoggerContext(
                    options.ApplicationName,
                    options.ApplicationInstanceId + "_" + options.BlobName,
                    new DateTimeOffset(key.Year, key.Month, key.Day, key.Hour, 0, 0, TimeSpan.Zero)));

                var blob = _blobReferenceFactory(blobName);

                using (var stream = new MemoryStream())
                using (var writer = new StreamWriter(stream))
                {
                    foreach (var logEvent in eventGroup)
                    {
                        writer.Write(logEvent.Message);
                    }

                    await writer.FlushAsync();
                    var tryGetBuffer = stream.TryGetBuffer(out var buffer);
                    Debug.Assert(tryGetBuffer);
                    await blob.AppendAsync(buffer, cancellationToken);
                }
            }
        }

        private (int Year, int Month, int Day, int Hour) GetBlobKey(LogMessage e)
        {
            return (e.Timestamp.Year,
                e.Timestamp.Month,
                e.Timestamp.Day,
                e.Timestamp.Hour);
        }
    }
}
