using AspNetCoreSdkTests.Templates;
using AspNetCoreSdkTests.Util;
using NUnit.Framework;

namespace AspNetCoreSdkTests
{
    [TestFixture]
    public class TemplateTests
    {
        [Test]
        public void Restore(
            [ValueSource(typeof(TemplateData), nameof(TemplateData.All))] Template template,
            [Values] NuGetConfig nuGetConfig)
        {
            using (var context = new DotNetContext())
            {
                context.New(template);
                context.Restore(nuGetConfig);

                CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterRestore, context.GetObjFiles());
            }
        }

        [Test]
        public void Build(
            [ValueSource(typeof(TemplateData), nameof(TemplateData.All))] Template template,
            [Values] NuGetConfig nuGetConfig)
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
