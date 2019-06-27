// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly string _appName;
        private readonly string _fileName;
        private readonly Func<string, ICloudAppendBlob> _blobReferenceFactory;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Creates a new instance of <see cref="BlobLoggerProvider"/>
        /// </summary>
        /// <param name="options">The options to use when creating a provider.</param>
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
            var value = options.CurrentValue;
            _appName = value.ApplicationName;
            _fileName = value.ApplicationInstanceId + "_" + value.BlobName;
            _blobReferenceFactory = blobReferenceFactory;
            _httpClient = new HttpClient();
        }

        internal override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken)
        {
            var eventGroups = messages.GroupBy(GetBlobKey);
            foreach (var eventGroup in eventGroups)
            {
                var key = eventGroup.Key;
                var blobName = $"{_appName}/{key.Year}/{key.Month:00}/{key.Day:00}/{key.Hour:00}/{_fileName}";

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
