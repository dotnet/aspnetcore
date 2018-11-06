// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging.AzureAppServices.Internal;

namespace Microsoft.Extensions.Logging.AzureAppServices
{
    /// <summary>
    /// Options for Azure diagnostics blob logging.
    /// </summary>
    public class AzureBlobLoggerOptions: BatchingLoggerOptions
    {
        public AzureBlobLoggerOptions()
        {
            BatchSize = 32;
        }

        private string _blobName = "applicationLog.txt";

        /// <summary>
        /// Gets or sets the last section of log blob name.
        /// Defaults to <c>"applicationLog.txt"</c>.
        /// </summary>
        public string BlobName
        {
            get { return _blobName; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(nameof(value), $"{nameof(BlobName)} must be non-empty string.");
                }
                _blobName = value;
            }
        }

        internal string ContainerUrl { get; set; }

        internal string ApplicationName { get; set; }

        internal string ApplicationInstanceId { get; set; }
    }
}