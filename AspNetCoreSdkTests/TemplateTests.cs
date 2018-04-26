using AspNetCoreSdkTests.Util;
using NUnit.Framework;

namespace AspNetCoreSdkTests
{
    [TestFixture]
    public class TemplateTests
    {
        [Test]
        [TestCaseSource(typeof(TestData), nameof(TestData.AllTemplates))]
        public void Restore(Template template, NuGetConfig nuGetConfig)
        {
            using (var context = new DotNetContext())
            {
                context.New(template, restore: false);
                context.Restore(nuGetConfig);
            }
        }
    }
}
