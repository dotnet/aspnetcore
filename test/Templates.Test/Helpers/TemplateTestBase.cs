// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.CommandLineUtils;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class TemplateTestBase : IDisposable
    {
        private static object DotNetNewLock = new object();

        protected string ProjectName { get; set; }
        protected string ProjectGuid { get; set; }
        protected string TemplateOutputDir { get; set; }
        protected ITestOutputHelper Output { get; private set; }

        public TemplateTestBase(ITestOutputHelper output)
        {
            TemplatePackageInstaller.EnsureTemplatePackagesWereReinstalled(output);

            Output = output;
            ProjectGuid = Guid.NewGuid().ToString("N");
            ProjectName = $"AspNet.Template.{ProjectGuid}";

            var assemblyPath = GetType().GetTypeInfo().Assembly.CodeBase;
            var assemblyUri = new Uri(assemblyPath, UriKind.Absolute);
            var basePath = Path.GetDirectoryName(assemblyUri.LocalPath);
            TemplateOutputDir = Path.Combine(basePath, "TestTemplates", ProjectName);
            Directory.CreateDirectory(TemplateOutputDir);

            // We don't want any of the host repo's build config interfering with
            // how the test project is built, so disconnect it from the
            // Directory.Build.props/targets context
            File.WriteAllText(Path.Combine(TemplateOutputDir, "Directory.Build.props"), "<Project><Import Project=\"../../TemplateTests.props\" /></Project>");
            File.WriteAllText(Path.Combine(TemplateOutputDir, "Directory.Build.targets"), "<Project />");
        }

        protected void InstallTemplatePackages()
        {
            throw new NotImplementedException();
        }

        protected void RunDotNetNew(string templateName, string targetFrameworkOverride, string auth = null, string language = null, bool useLocalDB = false, bool noHttps = false)
        {
            var args = $"new {templateName}";

            if (!string.IsNullOrEmpty(targetFrameworkOverride))
            {
                args += $" --target-framework-override {targetFrameworkOverride}";
            }

            if (!string.IsNullOrEmpty(auth))
            {
                args += $" -au {auth}";
            }

            if (!string.IsNullOrEmpty(language))
            {
                args += $" -lang {language}";
            }

            if (useLocalDB)
            {
                args += $" -uld";
            }

            if (noHttps)
            {
                args += $" --no-https";
            }

            // Only run one instance of 'dotnet new' at once, as a workaround for
            // https://github.com/aspnet/templating/issues/63
            lock (DotNetNewLock)
            {
                ProcessEx.Run(Output, TemplateOutputDir, DotNetMuxer.MuxerPathOrDefault(), args).WaitForExit(assertSuccess: true);
            }
        }

        protected void RunDotNet(string arguments)
        {
            lock (DotNetNewLock)
            {
                ProcessEx.Run(Output, TemplateOutputDir, DotNetMuxer.MuxerPathOrDefault(), arguments).WaitForExit(assertSuccess: true);
            }
        }

        protected void RunDotNetEfCreateMigration(string migrationName)
        {
            var args = $"ef migrations add {migrationName}";

            // Only run one instance of 'dotnet new' at once, as a workaround for
            // https://github.com/aspnet/templating/issues/63
            lock (DotNetNewLock)
            {
                ProcessEx.Run(Output, TemplateOutputDir, DotNetMuxer.MuxerPathOrDefault(), args).WaitForExit(assertSuccess: true);
            }
        }

        protected void AssertDirectoryExists(string path, bool shouldExist)
        {
            var fullPath = Path.Combine(TemplateOutputDir, path);
            var doesExist = Directory.Exists(fullPath);

            if (shouldExist)
            {
                Assert.True(doesExist, "Expected directory to exist, but it doesn't: " + path);
            }
            else
            {
                Assert.False(doesExist, "Expected directory not to exist, but it does: " + path);
            }
        }

        protected void AssertEmptyMigration(string migration)
        {
            var fullPath = Path.Combine(TemplateOutputDir, "Data/Migrations");
            var file = Directory.EnumerateFiles(fullPath).Where(f => f.EndsWith($"{migration}.cs")).FirstOrDefault();

            Assert.NotNull(file);
            var contents = File.ReadAllText(file);

            var emptyMigration = @"protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }";
            Assert.Contains(emptyMigration, contents);
        }

        protected void AssertFileExists(string path, bool shouldExist)
        {
            var fullPath = Path.Combine(TemplateOutputDir, path);
            var doesExist = File.Exists(fullPath);

            if (shouldExist)
            {
                Assert.True(doesExist, "Expected file to exist, but it doesn't: " + path);
            }
            else
            {
                Assert.False(doesExist, "Expected file not to exist, but it does: " + path);
            }
        }

        protected string ReadFile(string path)
        {
            AssertFileExists(path, shouldExist: true);
            return File.ReadAllText(Path.Combine(TemplateOutputDir, path));
        }

        protected AspNetProcess StartAspNetProcess(string targetFrameworkOverride, bool publish = false)
        {
            return new AspNetProcess(Output, TemplateOutputDir, ProjectName, targetFrameworkOverride, publish);
        }

        public void Dispose()
        {
            DeleteOutputDirectory();
        }

        private void DeleteOutputDirectory()
        {
            const int NumAttempts = 10;

            for (var numAttemptsRemaining = NumAttempts; numAttemptsRemaining > 0; numAttemptsRemaining--)
            {
                try
                {
                    Directory.Delete(TemplateOutputDir, true);
                    return;
                }
                catch (Exception ex)
                {
                    if (numAttemptsRemaining > 1)
                    {
                        Output.WriteLine($"Failed to delete directory {TemplateOutputDir} because of error {ex.Message}. Will try again {numAttemptsRemaining - 1} more time(s).");
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        Output.WriteLine($"Giving up trying to delete directory {TemplateOutputDir} after {NumAttempts} attempts. Most recent error was: {ex.StackTrace}");
                    }
                }
            }
        }
    }
}
