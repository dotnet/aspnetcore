using AspNetCoreSdkTests.Util;
using NUnit.Framework;
using System.Collections.Generic;

namespace AspNetCoreSdkTests
{
    [TestFixture]
    public class TemplateTests
    {
        [Test]
        [TestCaseSource(typeof(TestData), nameof(TestData.AllTemplates))]
        public void Restore(Template template, NuGetConfig nuGetConfig)
        {
            IEnumerable<string> objFiles;
            using (var context = new DotNetContext())
            {
                context.New(template, restore: false);
                context.Restore(nuGetConfig);
                objFiles = context.GetObjFiles();
            }

            var t = template.ToString().ToLowerInvariant();
            var expectedObjFiles = new[] {
                $"{t}.csproj.nuget.cache",
                $"{t}.csproj.nuget.g.props",
                $"{t}.csproj.nuget.g.targets",
                "project.assets.json",
            };

            CollectionAssert.AreEquivalent(expectedObjFiles, objFiles);
        }

        //[Test]
        //[TestCaseSource(typeof(TestData), nameof(TestData.AllTemplates))]
        //public void Build(Template template, NuGetConfig nuGetConfig)
        //{
        //    using (var context = new DotNetContext())
        //    {
        //        context.New(template, restore: false);
        //        context.Restore(nuGetConfig);
        //    }
        //}
    }
}
