// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Deployment.WindowsInstaller.Package;

namespace RepoTasks
{
    public class GetMsiProperty : Task
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
}
