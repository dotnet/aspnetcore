using AspNetCoreSdkTests.Templates;
using NUnit.Framework;
using System.Collections.Generic;

namespace AspNetCoreSdkTests
{
    [TestFixture]
    public class SelfContained
    {
        [Test]
        [TestCaseSource(nameof(RestoreData))]
        public void Restore(Template template)
        {
            CollectionAssert.AreEquivalent(template.ExpectedObjFilesAfterRestore, template.ObjFilesAfterRestore);
        }

        public static IEnumerable<Template> RestoreData = new[]
        {
            Template.GetInstance<ClassLibraryTemplate>(NuGetPackageSource.EnvironmentVariable),
            Template.GetInstance<ConsoleApplicationTemplate>(NuGetPackageSource.EnvironmentVariable),           
            Template.GetInstance<RazorClassLibraryTemplate>(NuGetPackageSource.EnvironmentVariable),
            Template.GetInstance<WebTemplate>(NuGetPackageSource.EnvironmentVariable),
            Template.GetInstance<RazorTemplate>(NuGetPackageSource.EnvironmentVariable),
            Template.GetInstance<MvcTemplate>(NuGetPackageSource.EnvironmentVariable),
            Template.GetInstance<AngularTemplate>(NuGetPackageSource.EnvironmentVariable),
            Template.GetInstance<ReactTemplate>(NuGetPackageSource.EnvironmentVariable),
            Template.GetInstance<ReactReduxTemplate>(NuGetPackageSource.EnvironmentVariable),
            Template.GetInstance<WebApiTemplate>(NuGetPackageSource.EnvironmentVariable),
        };
    }
}
