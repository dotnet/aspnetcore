// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.IIS.Core;

namespace Microsoft.AspNetCore.Server.IIS;

/// <summary>
/// Utility to access IIS details.
/// </summary>
public static class IISUtility
{
    /// <summary>
    /// Gets the <see cref="IIISEnvironmentFeature"/> for the current application if available.
    /// If possible, prefer <see cref="IServer.Features"/> to access this value.
    /// </summary>
    public static IIISEnvironmentFeature? GetEnvironmentFeature() => EnvironmentIISDetails.Create();
}
