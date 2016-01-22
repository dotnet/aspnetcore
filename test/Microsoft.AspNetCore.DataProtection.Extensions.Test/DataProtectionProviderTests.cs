// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.DataProtection.Test.Shared;
using Microsoft.AspNet.Testing.xunit;
using Xunit;

namespace Microsoft.AspNet.DataProtection
{
    public class DataProtectionProviderTests
    {
        [ConditionalFact]
        [ConditionalRunTestOnlyIfLocalAppDataAvailable]
        public void System_UsesProvidedDirectory()
        {
            WithUniqueTempDirectory(directory =>
            {
                // Step 1: directory should be completely empty
                directory.Create();
                Assert.Empty(directory.GetFiles());

                // Step 2: instantiate the system and round-trip a payload
                var protector = new DataProtectionProvider(directory).CreateProtector("purpose");
                Assert.Equal("payload", protector.Unprotect(protector.Protect("payload")));

                // Step 3: validate that there's now a single key in the directory and that it's not protected
                var allFiles = directory.GetFiles();
                Assert.Equal(1, allFiles.Length);
                Assert.StartsWith("key-", allFiles[0].Name, StringComparison.OrdinalIgnoreCase);
                string fileText = File.ReadAllText(allFiles[0].FullName);
                Assert.Contains("Warning: the key below is in an unencrypted form.", fileText, StringComparison.Ordinal);
                Assert.DoesNotContain("Windows DPAPI", fileText, StringComparison.Ordinal);
            });
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyIfLocalAppDataAvailable]
        [ConditionalRunTestOnlyOnWindows]
        public void System_UsesProvidedDirectory_WithConfigurationCallback()
        {
            WithUniqueTempDirectory(directory =>
            {
                // Step 1: directory should be completely empty
                directory.Create();
                Assert.Empty(directory.GetFiles());

                // Step 2: instantiate the system and round-trip a payload
                var protector = new DataProtectionProvider(directory, configure =>
                {
                    configure.ProtectKeysWithDpapi();
                }).CreateProtector("purpose");
                Assert.Equal("payload", protector.Unprotect(protector.Protect("payload")));

                // Step 3: validate that there's now a single key in the directory and that it's protected with DPAPI
                var allFiles = directory.GetFiles();
                Assert.Equal(1, allFiles.Length);
                Assert.StartsWith("key-", allFiles[0].Name, StringComparison.OrdinalIgnoreCase);
                string fileText = File.ReadAllText(allFiles[0].FullName);
                Assert.DoesNotContain("Warning: the key below is in an unencrypted form.", fileText, StringComparison.Ordinal);
                Assert.Contains("Windows DPAPI", fileText, StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// Runs a test and cleans up the temp directory afterward.
        /// </summary>
        private static void WithUniqueTempDirectory(Action<DirectoryInfo> testCode)
        {
            string uniqueTempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var dirInfo = new DirectoryInfo(uniqueTempPath);
            try
            {
                testCode(dirInfo);
            }
            finally
            {
                // clean up when test is done
                if (dirInfo.Exists)
                {
                    dirInfo.Delete(recursive: true);
                }
            }
        }

        private class ConditionalRunTestOnlyIfLocalAppDataAvailable : Attribute, ITestCondition
        {
            public bool IsMet => Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%") != null;

            public string SkipReason { get; } = "%LOCALAPPDATA% couldn't be located.";
        }
    }
}
