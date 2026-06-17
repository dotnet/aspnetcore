// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.Logging.AzureAppServices;

/// <summary>
/// Specifies options for Azure diagnostics blob logging.
/// </summary>
public class AzureBlobLoggerOptions : BatchingLoggerOptions
{
    private string _blobName = "applicationLog.txt";

    /// <summary>
    /// Gets or sets the last section of log blob name.
    /// </summary>
    /// <value>
    /// The last section of the log blob name. The default is <c>"applicationLog.txt"</c>.
    /// </value>
    public string BlobName
    {
        get { return _blobName; }
        set
        {
            ArgumentThrowHelper.ThrowIfNullOrEmpty(value);
            _blobName = value;
        }
    }

    /// <summary>
    /// Gets or sets the format of the file name.
    /// </summary>
    /// <value>
    /// The format of the file name. The default is "AppName/Year/Month/Day/Hour/Identifier".
    /// </value>
    public Func<AzureBlobLoggerContext, string> FileNameFormat { get; set; } = context =>
    {
        var timestamp = context.Timestamp;
        return $"{context.AppName}/{timestamp.Year}/{timestamp.Month:00}/{timestamp.Day:00}/{timestamp.Hour:00}/{context.Identifier}";
    };

    internal string ContainerUrl { get; set; }

    internal string ApplicationName { get; set; }

    internal string ApplicationInstanceId { get; set; }
}
