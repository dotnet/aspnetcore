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
    public sealed class W3CLoggerOptions : FileLoggerOptions
    {
        private string _fileName = "serverlog-";

        /// <summary>
        /// Fields to log. Defaults to logging request and response properties and headers.
        /// </summary>
        public W3CLoggingFields LoggingFields { get; set; } = W3CLoggingFields.Default;

        /// <summary>
        /// Gets or sets a string representing the prefix of the file name used to store the logging information.
        /// A GUID will be added after the given value.
        /// Defaults to <c>serverlog-</c>.
        /// </summary>
        public override string FileName
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
    }
}
