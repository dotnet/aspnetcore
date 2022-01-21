// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILD_MSI_TASKS
using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Deployment.WindowsInstaller.Package;

namespace RepoTasks;

public class GetMsiProperty : Microsoft.Build.Utilities.Task
{
    [Required]
    public string InstallPackage { get; set; }

    [Required]
    public string Property { get; set; }

    [Output]
    public string Value { get; set; }

    public override bool Execute()
    {
        try
        {
            using (var package = new InstallPackage(InstallPackage, 0))
            {
                Value = package.Property[Property];
            }
        }
        catch (Exception exception)
        {
            Log.LogErrorFromException(exception);
        }
        return !Log.HasLoggedErrors;
    }
}
#endif
