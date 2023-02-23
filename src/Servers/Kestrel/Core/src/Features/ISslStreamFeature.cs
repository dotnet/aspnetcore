// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features;

/// <summary>
/// Feature to get access the connection's <see cref="SslStream" />.
/// </summary>
public interface ISslStreamFeature
{
    /// <summary>
    /// Gets the <see cref="SslStream"/>.
    /// </summary>
    SslStream SslStream { get; }
}
