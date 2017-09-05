using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Templates.Test.Helpers;
using Xunit;

namespace Templates.Test
{
    public class TemplateTestBase : IDisposable
    {
        protected string ProjectName { get; set; }
        protected string TemplateOutputDir { get; private set; }

        // The tests use EdgeDriver because InternetExplorerDriver is flaky and
        // ChromeDriver is very slow (plus, Chrome might not be on CI machines).
        // This limits us to running the browser automation tests on Windows 10+.
        protected bool EnableBrowserAutomationTesting
            => OSSupportsEdge();

        static TemplateTestBase()
        {
            TemplatePackageInstaller.ReinstallTemplatePackages();
        }

        public TemplateTestBase()
        {
            ProjectName = Guid.NewGuid().ToString().Replace("-", "");

            var assemblyPath = GetType().GetTypeInfo().Assembly.CodeBase;
            var assemblyUri = new Uri(assemblyPath, UriKind.Absolute);
            var basePath = Path.GetDirectoryName(assemblyUri.LocalPath);
            TemplateOutputDir = Path.Combine(basePath, "TestTemplates", ProjectName);
            Directory.CreateDirectory(TemplateOutputDir);

            // We don't want any of the host repo's build config interfering with
            // how the test project is built, so disconnect it from the
            // Directory.Build.props/targets context
            File.WriteAllText(Path.Combine(TemplateOutputDir, "Directory.Build.props"), "<Project />");
            File.WriteAllText(Path.Combine(TemplateOutputDir, "Directory.Build.targets"), "<Project />");
        }

        protected void InstallTemplatePackages()
        {
            throw new NotImplementedException();
        }

        protected void RunDotNetNew(string templateName, string targetFrameworkOverride, string auth = null, string language = null)
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

            ProcessEx.Run(TemplateOutputDir, "dotnet", args).WaitForExit(assertSuccess: true);            
        }

        protected void RunNpmInstall()
        {
            // The first time this runs on any given CI agent it may take several minutes.
            // If the agent has NPM 5+ installed, it should be quite a lot quicker on
            // subsequent runs because of package caching.
            ProcessEx.Run(TemplateOutputDir, "cmd", "/c \"npm install\"").WaitForExit(assertSuccess: true);
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

        protected AspNetProcess StartAspNetProcess(string targetFrameworkOverride)
        {
            return new AspNetProcess(TemplateOutputDir, ProjectName, targetFrameworkOverride);
        }

        public void Dispose()
        {
            DeleteOutputDirectory();
        }

        private void DeleteOutputDirectory()
        {
            var numAttempts = 5;
            while (true)
            {
                try
                {
                    Directory.Delete(TemplateOutputDir, true);
                    return;
                }
                catch (IOException)
                {
                    numAttempts--;
                    if (numAttempts > 0)
                    {
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private static int GetWindowsVersion()
        {
            var osDescription = RuntimeInformation.OSDescription;
            var windowsVersion = Regex.Match(osDescription, "^Microsoft Windows (\\d+)\\..*");
            return windowsVersion.Success ? int.Parse(windowsVersion.Groups[1].Value) : -1;
        }

        private static bool OSSupportsEdge()
        {
            var windowsVersion = GetWindowsVersion();
            return (windowsVersion >= 10 && windowsVersion < 2000)
                || (windowsVersion >= 2016);
        }
    }
}
