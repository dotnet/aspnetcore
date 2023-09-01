// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IIS;

/// <summary>
/// This feature provides access to IIS application information
/// </summary>
public interface IIISEnvironmentFeature
{
    /// <summary>
    /// Gets the version of IIS that is being used.
    /// </summary>
    Version IISVersion { get; }

    /// <summary>
    /// Gets the AppPool name that is currently running
    /// </summary>
    string AppPoolId { get; }

    /// <summary>
    /// Gets the path to the AppPool config
    /// </summary>
    string AppPoolConfigFile { get; }

    /// <summary>
    /// Gets path to the application configuration that is currently running
    /// </summary>
    string AppConfigPath { get; }

    /// <summary>
    /// Gets the physical path of the application.
    /// </summary>
    string ApplicationPhysicalPath { get; }

    /// <summary>
    /// Gets the virtual path of the application.
    /// </summary>
    string ApplicationVirtualPath { get; }

    /// <summary>
    /// Gets ID of the current application.
    /// </summary>
    string ApplicationId { get; }

    /// <summary>
    /// Gets the name of the current site.
    /// </summary>
    string SiteName { get; }

    /// <summary>
    /// Gets the id of the current site.
    /// </summary>
    uint SiteId { get; }
}
