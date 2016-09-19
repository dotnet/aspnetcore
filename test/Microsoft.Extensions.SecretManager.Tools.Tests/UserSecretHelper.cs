// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Extensions.Configuration.UserSecrets.Tests
{
    internal class UserSecretHelper
    {
        internal static string GetTempSecretProject()
        {
            string userSecretsId;
            return GetTempSecretProject(out userSecretsId);
        }

        internal static string GetTempSecretProject(out string userSecretsId)
        {
            var projectPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "usersecretstest", Guid.NewGuid().ToString()));
            userSecretsId = Guid.NewGuid().ToString();
            File.WriteAllText(
                Path.Combine(projectPath.FullName, "project.json"),
                JsonConvert.SerializeObject(new { userSecretsId }));
            return projectPath.FullName;
        }

        internal static void SetTempSecretInProject(string projectPath, string userSecretsId)
        {
            File.WriteAllText(
                Path.Combine(projectPath, "project.json"),
                JsonConvert.SerializeObject(new { userSecretsId }));
        }

        internal static void DeleteTempSecretProject(string projectPath)
        {
            try
            {
                Directory.Delete(projectPath, true);
            }
            catch (Exception)
            {
                // Ignore failures.
            }
        }
    }
}