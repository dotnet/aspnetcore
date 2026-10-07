// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.AzureAppServices;

/// <summary>
/// Represents the context containing details for formatting the file name for the Azure blob logger.
/// </summary>
public readonly struct AzureBlobLoggerContext
{
    /// <summary>
    /// Creates a new <see cref="AzureBlobLoggerContext"/>.
    /// </summary>
    /// <param name="appName">The app name.</param>
    /// <param name="identifier">The file identifier.</param>
    /// <param name="timestamp">The timestamp.</param>
    public AzureBlobLoggerContext(string appName, string identifier, DateTimeOffset timestamp)
    {
        AppName = appName;
        Identifier = identifier;
        Timestamp = timestamp;
    }

    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public string AppName { get; }

    /// <summary>
    /// Gets the identifier for the log. This value is set to "<see cref="AzureBlobLoggerOptions.ApplicationInstanceId"/>_<see cref="AzureBlobLoggerOptions.BlobName"/>".
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets the timestamp representing when the log was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
