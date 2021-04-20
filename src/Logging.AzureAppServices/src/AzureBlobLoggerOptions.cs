// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.Logging.AzureAppServices
{
    /// <summary>
    /// Options for Azure diagnostics blob logging.
    /// </summary>
    public class AzureBlobLoggerOptions: BatchingLoggerOptions
    {
	    public Func<AzureBlobLoggerContext, string> FileNameFormat { get; set; }

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

        internal string CustomFileNamePrefix { get; set; }
       
        internal string ContainerUrl { get; set; }

        internal string ApplicationName { get; set; }

        internal string ApplicationInstanceId { get; set; }
    }
}
