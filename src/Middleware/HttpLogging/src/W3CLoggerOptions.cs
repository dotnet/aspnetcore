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
        private string _fileName = "serverlog-";
        private string _logDirectory = "";

        /// <summary>
        /// Fields to log. Defaults to logging request and response properties and headers.
        /// </summary>
        public W3CLoggingFields LoggingFields { get; set; } = W3CLoggingFields.Default;

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
        /// Gets or sets a string representing the prefix of the file name used to store the logging information.
        /// A GUID will be added after the given value.
        /// Defaults to <c>serverlog-</c>.
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
        /// Defaults to <c>something</c>.
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
    }
}
