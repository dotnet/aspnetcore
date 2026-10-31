// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

public class RemoteWindowsDeploymentParameters : DeploymentParameters
{
    public RemoteWindowsDeploymentParameters(
        string applicationPath,
        string dotnetRuntimePath,
        ServerType serverType,
        RuntimeFlavor runtimeFlavor,
        RuntimeArchitecture runtimeArchitecture,
        string remoteServerFileSharePath,
        string remoteServerName,
        string remoteServerAccountName,
        string remoteServerAccountPassword)
        : base(applicationPath, serverType, runtimeFlavor, runtimeArchitecture)
    {
        RemoteServerFileSharePath = remoteServerFileSharePath;
        ServerName = remoteServerName;
        ServerAccountName = remoteServerAccountName;
        ServerAccountPassword = remoteServerAccountPassword;
        DotnetRuntimePath = dotnetRuntimePath;
    }

    public string ServerName { get; }

    public string ServerAccountName { get; }

    public string ServerAccountPassword { get; }

    public string DotnetRuntimePath { get; }

    /// <summary>
    /// The full path to the remote server's file share
    /// </summary>
    public string RemoteServerFileSharePath { get; }
}
