// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal static class Constants
{
    public const int MaxExceptionDetailSize = 128;

    /// <summary>
    /// The endpoint Kestrel will bind to if nothing else is specified.
    /// </summary>
    public const string DefaultServerAddress = "http://localhost:5000";

    /// <summary>
    /// Prefix of host name used to specify Unix sockets in the configuration.
    /// </summary>
    public const string UnixPipeHostPrefix = "unix:/";

    /// <summary>
    /// Prefix of host name used to specify pipe file descriptor in the configuration.
    /// </summary>
    public const string PipeDescriptorPrefix = "pipefd:";

    /// <summary>
    /// Prefix of host name used to specify socket descriptor in the configuration.
    /// </summary>
    public const string SocketDescriptorPrefix = "sockfd:";

    public const string ServerName = "Kestrel";

    public static readonly TimeSpan RequestBodyDrainTimeout = TimeSpan.FromSeconds(5);
}
