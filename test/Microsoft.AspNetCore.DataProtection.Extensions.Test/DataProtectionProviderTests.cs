// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection
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
                var protector = DataProtectionProvider.Create(directory).CreateProtector("purpose");
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
        public void System_NoKeysDirectoryProvided_UsesDefaultKeysDirectory()
        {
            var keysPath = Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "ASP.NET", "DataProtection-Keys");
            var tempPath = Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "ASP.NET", "DataProtection-KeysTemp");

            try
            {
                // Step 1: Move the current contents, if any, to a temporary directory.
                if (Directory.Exists(keysPath))
                {
                    Directory.Move(keysPath, tempPath);
                }

                // Step 2: Instantiate the system and round-trip a payload
                var protector = DataProtectionProvider.Create("TestApplication").CreateProtector("purpose");
                Assert.Equal("payload", protector.Unprotect(protector.Protect("payload")));

                // Step 3: Validate that there's now a single key in the directory and that it's protected using Windows DPAPI.
                var newFileName = Assert.Single(Directory.GetFiles(keysPath));
                var file = new FileInfo(newFileName);
                Assert.StartsWith("key-", file.Name, StringComparison.OrdinalIgnoreCase);
                var fileText = File.ReadAllText(file.FullName);
                Assert.DoesNotContain("Warning: the key below is in an unencrypted form.", fileText, StringComparison.Ordinal);
                Assert.Contains("This key is encrypted with Windows DPAPI.", fileText, StringComparison.Ordinal);
            }
            finally
            {
                if (Directory.Exists(keysPath))
                {
                    Directory.Delete(keysPath, recursive: true);
                }
                if (Directory.Exists(tempPath))
                {
                    Directory.Move(tempPath, keysPath);
                }
            }
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
                var protector = DataProtectionProvider.Create(directory, configure =>
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

#if NET452 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml
        [ConditionalFact]
        [ConditionalRunTestOnlyIfLocalAppDataAvailable]
        [ConditionalRunTestOnlyOnWindows]
        public void System_UsesProvidedDirectoryAndCertificate()
        {
            var filePath = Path.Combine(GetTestFilesPath(), "TestCert.pfx");
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(filePath, "password"));
            store.Close();

            WithUniqueTempDirectory(directory =>
            {
                var certificateStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                certificateStore.Open(OpenFlags.ReadWrite);
                var certificate = certificateStore.Certificates.Find(X509FindType.FindBySubjectName, "TestCert", false)[0];

                try
                {
                    // Step 1: directory should be completely empty
                    directory.Create();
                    Assert.Empty(directory.GetFiles());

                    // Step 2: instantiate the system and round-trip a payload
                    var protector = DataProtectionProvider.Create(directory, certificate).CreateProtector("purpose");
                    Assert.Equal("payload", protector.Unprotect(protector.Protect("payload")));

                    // Step 3: validate that there's now a single key in the directory and that it's is protected using the certificate
                    var allFiles = directory.GetFiles();
                    Assert.Equal(1, allFiles.Length);
                    Assert.StartsWith("key-", allFiles[0].Name, StringComparison.OrdinalIgnoreCase);
                    string fileText = File.ReadAllText(allFiles[0].FullName);
                    Assert.DoesNotContain("Warning: the key below is in an unencrypted form.", fileText, StringComparison.Ordinal);
                    Assert.Contains("X509Certificate", fileText, StringComparison.Ordinal);
                }
                finally
                {
                    certificateStore.Remove(certificate);
                    certificateStore.Close();
                }
            });
        }
#elif NETCOREAPP2_0
#else
#error Target framework needs to be updated
#endif

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

        private static string GetTestFilesPath()
        {
            var projectName = typeof(DataProtectionProviderTests).GetTypeInfo().Assembly.GetName().Name;
            var projectPath = RecursiveFind(projectName, Path.GetFullPath("."));

            return Path.Combine(projectPath, projectName, "TestFiles");
        }

        private static string RecursiveFind(string path, string start)
        {
            var test = Path.Combine(start, path);
            if (Directory.Exists(test))
            {
                return start;
            }
            else
            {
                return RecursiveFind(path, new DirectoryInfo(start).Parent.FullName);
            }
        }
    }
}
