// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Connections;

/// <summary>
/// Sets up <see cref="ConnectionOptions"/>.
/// </summary>
public class ConnectionOptionsSetup : IConfigureOptions<ConnectionOptions>
{
    /// <summary>
    /// Default timeout value for disconnecting idle connections.
    /// </summary>
    /// <remarks>That's known typo issue, while we think it's not worth worth making a breaking change here, see https://github.com/dotnet/aspnetcore/pull/30558#discussion_r585189841</remarks>
    public static TimeSpan DefaultDisconectTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Sets default values for options if they have not been set yet.
    /// </summary>
    /// <param name="options">The <see cref="ConnectionOptions"/>.</param>
    public void Configure(ConnectionOptions options)
    {
        if (options.DisconnectTimeout == null)
        {
            options.DisconnectTimeout = DefaultDisconectTimeout;
        }
    }
}
