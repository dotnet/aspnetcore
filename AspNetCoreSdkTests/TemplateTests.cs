using AspNetCoreSdkTests.Templates;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace AspNetCoreSdkTests
{
    [TestFixture]
    public class TemplateTests
    {
        [Test]
        [TestCaseSource(nameof(RestoreData))]
        public void Restore(Template template)
        {
            CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterRestore, template.ObjFilesAfterRestore);
        }

        [Test]
        [TestCaseSource(nameof(BuildData))]
        public void Build(Template template)
        {
            CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterBuild, template.ObjFilesAfterBuild);
            CollectionAssert.AreEquivalent(template.ExpectedBinFilesAfterBuild, template.BinFilesAfterBuild);
        }

        [Test]
        [TestCaseSource(nameof(PublishData))]
        public void Publish(Template template)
        {
            CollectionAssert.AreEquivalent(template.ExpectedFilesAfterPublish, template.FilesAfterPublish);
        }

        [Test]
        [TestCaseSource(nameof(RunData))]
        public void Run(Template template)
        {
            var statusCode = template.HttpResponseAfterRun.StatusCode;
            Assert.AreEqual(HttpStatusCode.OK, statusCode,
                GetMessage(statusCode, template.ServerOutputAfterRun, template.ServerErrorAfterRun));

            statusCode = template.HttpsResponseAfterRun.StatusCode;
            Assert.AreEqual(HttpStatusCode.OK, statusCode,
                GetMessage(statusCode, template.ServerOutputAfterRun, template.ServerErrorAfterRun));
        }

        [NonParallelizable]
        [Test]
        [TestCaseSource(nameof(RunNonParallelizableData))]
        public void RunNonParallelizable(Template template)
        {
            Run(template);
        }

        [Test]
        [TestCaseSource(nameof(ExecData))]
        public void Exec(Template template)
        {
            var statusCode = template.HttpResponseAfterExec.StatusCode;
            Assert.AreEqual(HttpStatusCode.OK, statusCode,
                GetMessage(statusCode, template.ServerOutputAfterExec, template.ServerErrorAfterExec));

            statusCode = template.HttpsResponseAfterExec.StatusCode;
            Assert.AreEqual(HttpStatusCode.OK, statusCode,
                GetMessage(statusCode, template.ServerOutputAfterExec, template.ServerErrorAfterExec));
        }

        private static string GetMessage(HttpStatusCode statusCode, string serverOutput, string serverError)
        {
            return String.Join(Environment.NewLine,
                $"StatusCode: {statusCode}",
                string.Empty,
                "ServerOutput",
                "------------",
                serverOutput,
                string.Empty,
                "ServerError",
                "------------",
                serverError);
        }

        private static IEnumerable<Template> GetTemplates(RuntimeIdentifier runtimeIdentifier)
        {
            if (runtimeIdentifier == RuntimeIdentifier.None)
            {
                // Framework-dependent
                return new[]
                {
                    Template.GetInstance<ClassLibraryTemplate>(NuGetPackageSource.None, runtimeIdentifier),
                    Template.GetInstance<ConsoleApplicationTemplate>(NuGetPackageSource.None, runtimeIdentifier),
                    // Offline restore currently not supported for RazorClassLibrary template (https://github.com/aspnet/Universe/issues/1123)
                    Template.GetInstance<RazorClassLibraryTemplate>(NuGetPackageSource.NuGetOrg, runtimeIdentifier),
                    Template.GetInstance<WebTemplate>(NuGetPackageSource.None, runtimeIdentifier),
                    Template.GetInstance<RazorTemplate>(NuGetPackageSource.None, runtimeIdentifier),
                    Template.GetInstance<MvcTemplate>(NuGetPackageSource.None, runtimeIdentifier),
                    Template.GetInstance<AngularTemplate>(NuGetPackageSource.None, runtimeIdentifier),
                    Template.GetInstance<ReactTemplate>(NuGetPackageSource.None, runtimeIdentifier),
                    Template.GetInstance<ReactReduxTemplate>(NuGetPackageSource.None, runtimeIdentifier),
                    Template.GetInstance<WebApiTemplate>(NuGetPackageSource.None, runtimeIdentifier),
                };
            }
            else
            {
                // Self-contained
                return new[]
                {
                    // ClassLibrary does not require a package source, even for self-contained deployments
                    Template.GetInstance<ClassLibraryTemplate>(NuGetPackageSource.None, runtimeIdentifier),
                    Template.GetInstance<ConsoleApplicationTemplate>(NuGetPackageSource.DotNetCore, runtimeIdentifier),
                    Template.GetInstance<RazorClassLibraryTemplate>(NuGetPackageSource.NuGetOrg, runtimeIdentifier),
                    Template.GetInstance<WebTemplate>(NuGetPackageSource.DotNetCore, runtimeIdentifier),
                    Template.GetInstance<RazorTemplate>(NuGetPackageSource.DotNetCore, runtimeIdentifier),
                    Template.GetInstance<MvcTemplate>(NuGetPackageSource.DotNetCore, runtimeIdentifier),
                    Template.GetInstance<AngularTemplate>(NuGetPackageSource.DotNetCore, runtimeIdentifier),
                    Template.GetInstance<ReactTemplate>(NuGetPackageSource.DotNetCore, runtimeIdentifier),
                    Template.GetInstance<ReactReduxTemplate>(NuGetPackageSource.DotNetCore, runtimeIdentifier),
                    Template.GetInstance<WebApiTemplate>(NuGetPackageSource.DotNetCore, runtimeIdentifier),
                };
            }
        }

        private static readonly IEnumerable<Template> _restoreTemplates = RuntimeIdentifier.All.SelectMany(r => GetTemplates(r));

        // Must call ToList() or similar on RestoreData to ensure TestCaseData instances can be compared to each other,
        // which is required to use Except() in RunData.
        public static IEnumerable<TestCaseData> RestoreData = _restoreTemplates.Select(t => new TestCaseData(t)).ToList();

        public static IEnumerable<TestCaseData> BuildData => RestoreData;

        public static IEnumerable<TestCaseData> PublishData => BuildData;

        private static readonly IEnumerable<TestCaseData> _runData =
            from tcd in BuildData
            let t = (Template)tcd.Arguments[0]
            // Only interested in verifying web applications
            where (t.Type == TemplateType.WebApplication)
            // "dotnet run" is only relevant for framework-dependent apps
            where (t.RuntimeIdentifier == RuntimeIdentifier.None)
            select tcd;

        // On Linux, calling "dotnet run" on multiple React templates in parallel may fail since the default
        // fs.inotify.max_user_watches is too low.  One workaround is to increase fs.inotify.max_user_watches,
        // but this means tests will fail on a default machine.  A simpler workaround is to disable parallel
        // execution for these tests.
        public static IEnumerable<TestCaseData> RunNonParallelizableData =
            from tcd in _runData
            let t = (Template)tcd.Arguments[0]
            where (t is ReactTemplate)
            select tcd;

        public static IEnumerable<TestCaseData> RunData = _runData.Except(RunNonParallelizableData);

        public static IEnumerable<TestCaseData> ExecData =
            from tcd in PublishData
            let t = (Template)tcd.Arguments[0]
            // Only interested in verifying web applications
            where (t.Type == TemplateType.WebApplication)
            // Can only run framework-dependent apps and self-contained apps matching the current platform
            let runnable = t.RuntimeIdentifier.OSPlatforms.Any(p => RuntimeInformation.IsOSPlatform(p))
            select (runnable ? tcd : tcd.Ignore($"RuntimeIdentifier '{t.RuntimeIdentifier}' cannot be executed on this platform"));
    }
}
