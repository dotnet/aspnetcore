// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Cli.FunctionalTests.Templates;
using Cli.FunctionalTests.Util;
using NuGet.Versioning;
using NUnit.Framework;

namespace Cli.FunctionalTests
{
    [TestFixture]
    public class TemplateTests
    {
        [Test]
        [TestCaseSource(nameof(RestoreData))]
        public void _1_Restore(Template template)
        {
            var expected = template.ExpectedObjFilesAfterRestore;
            var actual = template.ObjFilesAfterRestore;
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        [TestCaseSource(nameof(RestoreData))]
        public void _2_RestoreIncremental(Template template)
        {
            var expected = template.ExpectedObjFilesAfterRestore;
            var actual = template.ObjFilesAfterRestoreIncremental;
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        [TestCaseSource(nameof(BuildData))]
        public void _3_Build(Template template)
        {
            var expectedObj = template.ExpectedObjFilesAfterBuild;
            var actualObj = template.ObjFilesAfterBuild;
            CollectionAssert.AreEquivalent(expectedObj, actualObj);

            var expectedBin = template.ExpectedBinFilesAfterBuild;
            var actualBin = template.BinFilesAfterBuild;
            CollectionAssert.AreEquivalent(expectedBin, actualBin);
        }

        [Test]
        [TestCaseSource(nameof(BuildData))]
        public void _4_BuildIncremental(Template template)
        {
            var expectedObj = template.ExpectedObjFilesAfterBuild;
            var actualObj = template.ObjFilesAfterBuildIncremental;
            CollectionAssert.AreEquivalent(expectedObj, actualObj);

            var expectedBin = template.ExpectedBinFilesAfterBuild;
            var actualBin = template.BinFilesAfterBuildIncremental;
            CollectionAssert.AreEquivalent(expectedBin, actualBin);
        }

        [Test]
        [TestCaseSource(nameof(RunData))]
        public void _5_Run(Template template)
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
        public void _5_RunNonParallelizable(Template template)
        {
            _5_Run(template);
        }

        [Test]
        [TestCaseSource(nameof(PublishData))]
        public void _6_Publish(Template template)
        {
            var expected = template.ExpectedFilesAfterPublish;
            var actual = template.FilesAfterPublish;
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        [TestCaseSource(nameof(PublishData))]
        public void _7_PublishIncremental(Template template)
        {
            var expected = template.ExpectedFilesAfterPublish;
            var actual = template.FilesAfterPublishIncremental;
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        [TestCaseSource(nameof(ExecData))]
        public void _8_Exec(Template template)
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
            // Offline restore is broken in SDK 2.1.301 (https://github.com/aspnet/Universe/issues/1220)
            var offlinePackageSource = (DotNetUtil.SdkVersion == new SemanticVersion(2, 1, 301)) ?
                NuGetPackageSource.NuGetOrg : NuGetPackageSource.None;

            // Pre-release SDKs require a private nuget feed
            var onlinePackageSource = DotNetUtil.RequiresPrivateFeed ?
                NuGetPackageSource.EnvironmentVariableAndNuGetOrg : NuGetPackageSource.NuGetOrg;

            if (runtimeIdentifier == RuntimeIdentifier.None)
            {
                // Framework-dependent
                return new[]
                {
                    Template.GetInstance<ClassLibraryTemplate>(NuGetPackageSource.None, runtimeIdentifier),
                    Template.GetInstance<ConsoleApplicationTemplate>(offlinePackageSource, runtimeIdentifier),
                    
                    // Offline restore currently not supported for RazorClassLibrary template (https://github.com/aspnet/Universe/issues/1123)
                    Template.GetInstance<RazorClassLibraryTemplate>(onlinePackageSource, runtimeIdentifier),

                    Template.GetInstance<WebTemplate>(offlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<RazorTemplate>(offlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<MvcTemplate>(offlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<AngularTemplate>(offlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<ReactTemplate>(offlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<ReactReduxTemplate>(offlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<WebApiTemplate>(offlinePackageSource, runtimeIdentifier),
                };
            }
            else
            {
                // Self-contained
                return new[]
                {
                    // ClassLibrary does not require a package source, even for self-contained deployments
                    Template.GetInstance<ClassLibraryTemplate>(NuGetPackageSource.None, runtimeIdentifier),

                    Template.GetInstance<ConsoleApplicationTemplate>(onlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<RazorClassLibraryTemplate>(onlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<WebTemplate>(onlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<RazorTemplate>(onlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<MvcTemplate>(onlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<AngularTemplate>(onlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<ReactTemplate>(onlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<ReactReduxTemplate>(onlinePackageSource, runtimeIdentifier),
                    Template.GetInstance<WebApiTemplate>(onlinePackageSource, runtimeIdentifier),
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
