// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.DataProtection.Repositories;

internal sealed class DefaultKeyStorageDirectories : IDefaultKeyStorageDirectories
{
    private static readonly Lazy<DirectoryInfo?> _defaultDirectoryLazy = new Lazy<DirectoryInfo?>(GetKeyStorageDirectoryImpl);

    private DefaultKeyStorageDirectories()
    {
    }

    public static IDefaultKeyStorageDirectories Instance { get; } = new DefaultKeyStorageDirectories();

    /// <summary>
    /// The default key storage directory.
    /// On Windows, this currently corresponds to "Environment.SpecialFolder.LocalApplication/ASP.NET/DataProtection-Keys".
    /// On Linux and macOS, this currently corresponds to "$HOME/.aspnet/DataProtection-Keys".
    /// </summary>
    /// <remarks>
    /// This property can return null if no suitable default key storage directory can
    /// be found, such as the case when the user profile is unavailable.
    /// </remarks>
    public DirectoryInfo? GetKeyStorageDirectory() => _defaultDirectoryLazy.Value;

    private static DirectoryInfo? GetKeyStorageDirectoryImpl()
    {
        DirectoryInfo retVal;

        // Environment.GetFolderPath returns null if the user profile isn't loaded.
        var localAppDataFromSystemPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var localAppDataFromEnvPath = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !string.IsNullOrEmpty(localAppDataFromSystemPath))
        {
            // To preserve backwards-compatibility with 1.x, Environment.SpecialFolder.LocalApplicationData
            // cannot take precedence over $LOCALAPPDATA and $HOME/.aspnet on non-Windows platforms
            retVal = GetKeyStorageDirectoryFromBaseAppDataPath(localAppDataFromSystemPath);
        }
        else if (localAppDataFromEnvPath != null)
        {
            retVal = GetKeyStorageDirectoryFromBaseAppDataPath(localAppDataFromEnvPath);
        }
        else if (homePath != null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                retVal = GetKeyStorageDirectoryFromBaseAppDataPath(Path.Combine(homePath, "AppData", "Local"));
            }
            else
            {
                // Use*NIX conventions for a folder name.
                retVal = new DirectoryInfo(Path.Combine(homePath, ".aspnet", DataProtectionKeysFolderName));
            }
        }
        else if (!string.IsNullOrEmpty(localAppDataFromSystemPath))
        {
            // Starting in 2.x, non-Windows platforms may use Environment.SpecialFolder.LocalApplicationData
            // but only after checking for $LOCALAPPDATA, $USERPROFILE, and $HOME.
            retVal = GetKeyStorageDirectoryFromBaseAppDataPath(localAppDataFromSystemPath);
        }
        else
        {
            return null;
        }

        Debug.Assert(retVal != null);

        try
        {
            retVal.Create(); // throws if we don't have access, e.g., user profile not loaded
            return retVal;
        }
        catch
        {
            return null;
        }
    }

    public DirectoryInfo? GetKeyStorageDirectoryForAzureWebSites()
    {
        // Azure Web Sites needs to be treated specially, as we need to store the keys in a
        // correct persisted location. We use the existence of the %WEBSITE_INSTANCE_ID% env
        // variable to determine if we're running in this environment, and if so we then use
        // the %HOME% variable to build up our base key storage path.
        if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
        {
            var homeEnvVar = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(homeEnvVar))
            {
                return GetKeyStorageDirectoryFromBaseAppDataPath(homeEnvVar);
            }
        }

        // nope
        return null;
    }

    private const string DataProtectionKeysFolderName = "DataProtection-Keys";

    private static DirectoryInfo GetKeyStorageDirectoryFromBaseAppDataPath(string basePath)
    {
        return new DirectoryInfo(Path.Combine(basePath, "ASP.NET", DataProtectionKeysFolderName));
    }
}
