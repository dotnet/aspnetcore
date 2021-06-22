// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// Options for the <see cref="W3CLogger"/>.
    /// </summary>
    public sealed class W3CLoggerOptions
    {
        private int? _fileSizeLimit = 10 * 1024 * 1024;
        private int? _retainedFileCountLimit = 4;
        private string _fileName = "w3clog-";
        private string _logDirectory = "";
        private TimeSpan _flushPeriod = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets a strictly positive value representing the maximum log size in bytes or null for no limit.
        /// Once the log is full, no more messages will be appended.
        /// Defaults to <c>10MiB</c>.
        /// </summary>
        public int? FileSizeLimit
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
        /// Gets or sets a strictly positive value representing the maximum retained file count or null for no limit.
        /// Defaults to <c>4</c>.
        /// </summary>
        public int? RetainedFileCountLimit
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
        /// Gets or sets a string representing the prefix of the file name used to store the logging information.
        /// The current date plus a file number (in the format {YYYYMMDD.X} will be appended to the given value.
        /// Defaults to <c>w3clog-</c>.
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _fileName = value;
            }
        }

        /// <summary>
        /// Gets or sets a string representing the directory where the log file will be written to
        /// Defaults to <c>/logs/</c> relative to the app directory (ContentRoot).
        /// </summary>
        public string LogDirectory
        {
            get { return _logDirectory; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _logDirectory = value;
            }
        }

        /// <summary>
        /// Gets or sets the period after which logs will be flushed to the store.
        /// Defaults to 1 second.
        /// </summary>
        public TimeSpan FlushPeriod
        {
            get { return _flushPeriod; }
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(FlushPeriod)} must be positive.");
                }
                _flushPeriod = value;
            }
        }

        /// <summary>
        /// Fields to log. Defaults to logging request and response properties and headers,
        /// plus date/time info and server name.
        /// </summary>
        public W3CLoggingFields LoggingFields { get; set; } = W3CLoggingFields.Date | W3CLoggingFields.Time |
            W3CLoggingFields.ServerName | W3CLoggingFields.Method | W3CLoggingFields.UriStem | W3CLoggingFields.UriQuery |
            W3CLoggingFields.ProtocolStatus | W3CLoggingFields.TimeTaken | W3CLoggingFields.ProtocolVersion |
            W3CLoggingFields.Host | W3CLoggingFields.UserAgent | W3CLoggingFields.Referer | W3CLoggingFields.ConnectionInfoFields;

    }
}
