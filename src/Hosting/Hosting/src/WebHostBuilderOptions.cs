// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Builder options for use with ConfigureWebHost.
/// </summary>
public class WebHostBuilderOptions
{
    /// <summary>
    /// Indicates if "ASPNETCORE_" prefixed environment variables should be added to configuration.
    /// They are added by default.
    /// </summary>
    public bool SuppressEnvironmentConfiguration { get; set; }
}
