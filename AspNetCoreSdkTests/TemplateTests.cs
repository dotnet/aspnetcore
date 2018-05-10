using AspNetCoreSdkTests.Templates;
using NUnit.Framework;
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
            Assert.AreEqual(HttpStatusCode.OK, template.HttpResponseAfterRun.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, template.HttpsResponseAfterRun.StatusCode);
        }

        [Test]
        [TestCaseSource(nameof(ExecData))]
        public void Exec(Template template)
        {
            Assert.AreEqual(HttpStatusCode.OK, template.HttpResponseAfterExec.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, template.HttpsResponseAfterExec.StatusCode);
        }

        private static IEnumerable<Template> _restoreTemplates = new[]
        {
            // Framework-dependent
            Template.GetInstance<ClassLibraryTemplate>(NuGetPackageSource.None, RuntimeIdentifier.None),
            Template.GetInstance<ConsoleApplicationTemplate>(NuGetPackageSource.None, RuntimeIdentifier.None),
            // Offline restore currently not supported for RazorClassLibrary template (https://github.com/aspnet/Universe/issues/1123)
            Template.GetInstance<RazorClassLibraryTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.None),
            Template.GetInstance<WebTemplate>(NuGetPackageSource.None, RuntimeIdentifier.None),
            Template.GetInstance<RazorTemplate>(NuGetPackageSource.None, RuntimeIdentifier.None),
            Template.GetInstance<MvcTemplate>(NuGetPackageSource.None, RuntimeIdentifier.None),
            Template.GetInstance<AngularTemplate>(NuGetPackageSource.None, RuntimeIdentifier.None),
            Template.GetInstance<ReactTemplate>(NuGetPackageSource.None, RuntimeIdentifier.None),
            Template.GetInstance<ReactReduxTemplate>(NuGetPackageSource.None, RuntimeIdentifier.None),
            Template.GetInstance<WebApiTemplate>(NuGetPackageSource.None, RuntimeIdentifier.None),

            // Self-contained, win-x64
            // ClassLibrary does not require a package source, even for self-contained deployments
            Template.GetInstance<ClassLibraryTemplate>(NuGetPackageSource.None, RuntimeIdentifier.Win_x64),
            Template.GetInstance<ConsoleApplicationTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<RazorClassLibraryTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<WebTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<RazorTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<MvcTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<AngularTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<ReactTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<ReactReduxTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<WebApiTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),

            // Self-contained, linux-x64
            // ClassLibrary does not require a package source, even for self-contained deployments
            Template.GetInstance<ClassLibraryTemplate>(NuGetPackageSource.None, RuntimeIdentifier.Linux_x64),
            Template.GetInstance<ConsoleApplicationTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Linux_x64),
            Template.GetInstance<RazorClassLibraryTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Linux_x64),
            Template.GetInstance<WebTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Linux_x64),
            Template.GetInstance<RazorTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Linux_x64),
            Template.GetInstance<MvcTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Linux_x64),
            Template.GetInstance<AngularTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Linux_x64),
            Template.GetInstance<ReactTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Linux_x64),
            Template.GetInstance<ReactReduxTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Linux_x64),
            Template.GetInstance<WebApiTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Linux_x64),

            // Self-contained, osx-x64
            // ClassLibrary does not require a package source, even for self-contained deployments
            Template.GetInstance<ClassLibraryTemplate>(NuGetPackageSource.None, RuntimeIdentifier.OSX_x64),
            Template.GetInstance<ConsoleApplicationTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.OSX_x64),
            Template.GetInstance<RazorClassLibraryTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.OSX_x64),
            Template.GetInstance<WebTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.OSX_x64),
            Template.GetInstance<RazorTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.OSX_x64),
            Template.GetInstance<MvcTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.OSX_x64),
            Template.GetInstance<AngularTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.OSX_x64),
            Template.GetInstance<ReactTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.OSX_x64),
            Template.GetInstance<ReactReduxTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.OSX_x64),
            Template.GetInstance<WebApiTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.OSX_x64),
        };

        public static IEnumerable<TestCaseData> RestoreData = _restoreTemplates.Select(t => new TestCaseData(t));

        public static IEnumerable<TestCaseData> BuildData => RestoreData;

        public static IEnumerable<TestCaseData> PublishData => BuildData;

        public static IEnumerable<TestCaseData> RunData =
            from tcd in BuildData
            let t = (Template)tcd.Arguments[0]
            // Only interested in verifying web applications
            where (t.Type == TemplateType.WebApplication)
            // "dotnet run" is only relevant for framework-dependent apps
            where (t.RuntimeIdentifier == RuntimeIdentifier.None)
            select tcd;

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
