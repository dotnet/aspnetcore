using AspNetCoreSdkTests.Templates;
using NUnit.Framework;
using System.Net;

namespace AspNetCoreSdkTests
{
    [TestFixture]
    public class TemplateTests
    {
        [Test]
        [TestCaseSource(typeof(TemplateData), nameof(TemplateData.Restore))]
        public void Restore(Template template)
        {
            CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterRestore, template.ObjFilesAfterRestore);
        }

        [Test]
        [TestCaseSource(typeof(TemplateData), nameof(TemplateData.Build))]
        public void Build(Template template)
        {
            CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterBuild, template.ObjFilesAfterBuild);
        }

        [Test]
        [TestCaseSource(typeof(TemplateData), nameof(TemplateData.Publish))]
        public void Publish(Template template)
        {
            CollectionAssert.AreEquivalent(template.ExpectedFilesAfterPublish, template.FilesAfterPublish);
        }

        [Test]
        [TestCaseSource(typeof(TemplateData), nameof(TemplateData.Run))]
        public void Run(Template template)
        {
            Assert.AreEqual(HttpStatusCode.OK, template.HttpResponseAfterRun.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, template.HttpsResponseAfterRun.StatusCode);
        }

        [Test]
        [TestCaseSource(typeof(TemplateData), nameof(TemplateData.Exec))]
        public void Exec(Template template)
        {
            Assert.AreEqual(HttpStatusCode.OK, template.HttpResponseAfterExec.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, template.HttpsResponseAfterExec.StatusCode);
        }
    }
}
