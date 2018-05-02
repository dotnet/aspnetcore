using AspNetCoreSdkTests.Templates;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace AspNetCoreSdkTests
{
    [TestFixture]
    public class FrameworkDependent
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
            Template.GetInstance<ClassLibraryTemplate>(NuGetPackageSource.None),
            Template.GetInstance<ConsoleApplicationTemplate>(NuGetPackageSource.None),
            
            // Offline restore currently not supported for RazorClassLibrary template (https://github.com/aspnet/Universe/issues/1123)
            Template.GetInstance<RazorClassLibraryTemplate>(NuGetPackageSource.NuGetOrg),

            Template.GetInstance<WebTemplate>(NuGetPackageSource.None),
            Template.GetInstance<RazorTemplate>(NuGetPackageSource.None),
            Template.GetInstance<MvcTemplate>(NuGetPackageSource.None),
            Template.GetInstance<AngularTemplate>(NuGetPackageSource.None),
            Template.GetInstance<ReactTemplate>(NuGetPackageSource.None),
            Template.GetInstance<ReactReduxTemplate>(NuGetPackageSource.None),
            Template.GetInstance<WebApiTemplate>(NuGetPackageSource.None),
        };

        public static IEnumerable<Template> BuildData => RestoreData;

        public static IEnumerable<Template> PublishData => RestoreData;

        public static IEnumerable<Template> RunData = RestoreData.Where(t => t.Type == TemplateType.WebApplication);

        public static IEnumerable<Template> ExecData => RunData;
    }
}
