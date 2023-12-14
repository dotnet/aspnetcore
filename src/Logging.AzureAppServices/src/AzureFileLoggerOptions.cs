// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.Logging.AzureAppServices;

/// <summary>
/// Options for Azure diagnostics file logging.
/// </summary>
public class AzureFileLoggerOptions : BatchingLoggerOptions
{
    private int? _fileSizeLimit = 10 * 1024 * 1024;
    private int? _retainedFileCountLimit = 2;
    private string _fileName = "diagnostics-";

    /// <summary>
    /// Gets or sets a strictly positive value representing the maximum log size in bytes or null for no limit.
    /// Once the log is full, no more messages will be appended.
    /// Defaults to <c>10MB</c>.
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
    /// Defaults to <c>2</c>.
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
    /// The current date, in the format YYYYMMDD will be added after the given value.
    /// Defaults to <c>diagnostics-</c>.
    /// </summary>
    public string FileName
    {
        get { return _fileName; }
        set
        {
            ArgumentThrowHelper.ThrowIfNullOrEmpty(value);

            _fileName = value;
        }
    }

    internal string LogDirectory { get; set; }
}
