// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.AzureAppServices
{
    /// <summary>
    /// Settings for Azure diagnostics logging.
    /// </summary>
    public class AzureAppServicesDiagnosticsSettings
    {
        private TimeSpan _blobCommitPeriod = TimeSpan.FromSeconds(5);
        private int _blobBatchSize = 32;
        private string _outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";
        private int _retainedFileCountLimit = 2;
        private int _fileSizeLimit = 10 * 1024 * 1024;
        private string _blobName = "applicationLog.txt";
        private TimeSpan? _fileFlushPeriod = TimeSpan.FromSeconds(1);
        private int _backgroundQueueSize;

        /// <summary>
        /// Gets or sets a strictly positive value representing the maximum log size in bytes.
        /// Once the log is full, no more messages will be appended.
        /// Defaults to <c>10MB</c>.
        /// </summary>
        public int FileSizeLimit
        {
            get { return _fileSizeLimit; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(FileSizeLimit)} must be positive.");
                }
                _fileSizeLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets a strictly positive value representing the maximum retained file count.
        /// Defaults to <c>2</c>.
        /// </summary>
        public int RetainedFileCountLimit
        {
            get { return _retainedFileCountLimit; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(RetainedFileCountLimit)} must be positive.");
                }
                _retainedFileCountLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets a message template describing the output messages.
        /// Defaults to <c>"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}"</c>.
        /// </summary>
        public string OutputTemplate
        {
            get { return _outputTemplate; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(nameof(value), $"{nameof(OutputTemplate)} must be non-empty string.");
                }
                _outputTemplate = value;
            }
        }

        /// <summary>
        /// Gets or sets a maximum number of events to include in a single blob append batch.
        /// Defaults to <c>32</c>.
        /// </summary>
        public int BlobBatchSize
        {
            get { return _blobBatchSize; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(BlobBatchSize)} must be positive.");
                }
                _blobBatchSize = value;
            }
        }

        /// <summary>
        /// Gets or sets a time to wait between checking for blob log batches.
        /// Defaults to 5 seconds.
        /// </summary>
        public TimeSpan BlobCommitPeriod
        {
            get { return _blobCommitPeriod; }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(BlobCommitPeriod)} must be positive.");
                }
                _blobCommitPeriod = value;
            }
        }

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

        /// <summary>
        /// Gets or sets the maximum size of the background log message queue or 0 for no limit.
        /// After maximum queue size is reached log event sink would start blocking.
        /// Defaults to <c>0</c>.
        /// </summary>
        public int BackgroundQueueSize
        {
            get { return _backgroundQueueSize; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(BackgroundQueueSize)} must be non-negative.");
                }
                _backgroundQueueSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the period after which logs will be flushed to disk or
        /// <c>null</c> if auto flushing is not required.
        /// Defaults to 1 second.
        /// </summary>
        public TimeSpan? FileFlushPeriod
        {
            get { return _fileFlushPeriod; }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(FileFlushPeriod)} must be positive.");
                }
                _fileFlushPeriod = value;
            }
        }
    }
}