using AspNetCoreSdkTests.Templates;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net;

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

        public static IEnumerable<Template> RestoreData = new[]
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

            // Self-contained
            // ClassLibrary does not require a package source, even for self-contained deployments
            Template.GetInstance<ClassLibraryTemplate>(NuGetPackageSource.None, RuntimeIdentifier.Win_x64),
            Template.GetInstance<ConsoleApplicationTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            // Offline restore currently not supported for RazorClassLibrary template (https://github.com/aspnet/Universe/issues/1123)
            Template.GetInstance<RazorClassLibraryTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<WebTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<RazorTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<MvcTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<AngularTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<ReactTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<ReactReduxTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
            Template.GetInstance<WebApiTemplate>(NuGetPackageSource.NuGetOrg, RuntimeIdentifier.Win_x64),
        };

        public static IEnumerable<Template> BuildData => RestoreData;

        public static IEnumerable<Template> PublishData => BuildData;

        public static IEnumerable<Template> RunData =
            BuildData.
            // Only interested in verifying web applications
            Where(t => t.Type == TemplateType.WebApplication).
            // "dotnet run" is only relevant for framework-dependent apps
            Where(t => t.RuntimeIdentifier == RuntimeIdentifier.None);

        public static IEnumerable<Template> ExecData =
            PublishData.
            // Only interested in verifying web applications
            Where(t => t.Type == TemplateType.WebApplication);
    }
}
