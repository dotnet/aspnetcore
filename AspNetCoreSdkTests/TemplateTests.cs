using AspNetCoreSdkTests.Templates;
using AspNetCoreSdkTests.Util;
using NUnit.Framework;

namespace AspNetCoreSdkTests
{
    [TestFixture]
    public class TemplateTests
    {
        [Test]
        [TestCaseSource(typeof(TemplateData), nameof(TemplateData.Current))]
        public void Restore(Template template, NuGetConfig nuGetConfig)
        {
            using (var context = new DotNetContext())
            {
                context.New(template);
                context.Restore(nuGetConfig);

                CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterRestore, context.GetObjFiles());
            }
        }

        [Test]
        [TestCaseSource(typeof(TemplateData), nameof(TemplateData.Current))]
        public void Build(Template template, NuGetConfig nuGetConfig)
        {
            using (var context = new DotNetContext())
            {
                context.New(template);
                context.Restore(nuGetConfig);
                context.Build();

                CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterBuild, context.GetObjFiles());
                CollectionAssert.AreEquivalent(template.ExpectedBinFilesAfterBuild, context.GetBinFiles());
            }
        }
    }
}
