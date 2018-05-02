using AspNetCoreSdkTests.Templates;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreSdkTests
{
    public static class TemplateData
    {
        public static IEnumerable<TestCaseData> Restore = new TestCaseData[]
        {
            new TestCaseData(Template.GetInstance<ClassLibraryTemplate>(NuGetConfig.Empty)),
            new TestCaseData(Template.GetInstance<ConsoleApplicationTemplate>(NuGetConfig.Empty)),
            
            // Offline restore currently not supported for RazorClassLibrary template (https://github.com/aspnet/Universe/issues/1123)
            new TestCaseData(Template.GetInstance<RazorClassLibraryTemplate>(NuGetConfig.NuGetOrg)),

            new TestCaseData(Template.GetInstance<WebTemplate>(NuGetConfig.Empty)),
            new TestCaseData(Template.GetInstance<RazorTemplate>(NuGetConfig.Empty)),
            new TestCaseData(Template.GetInstance<MvcTemplate>(NuGetConfig.Empty)),
            new TestCaseData(Template.GetInstance<AngularTemplate>(NuGetConfig.Empty)),
            new TestCaseData(Template.GetInstance<ReactTemplate>(NuGetConfig.Empty)),
            new TestCaseData(Template.GetInstance<ReactReduxTemplate>(NuGetConfig.Empty)),
            new TestCaseData(Template.GetInstance<WebApiTemplate>(NuGetConfig.Empty)),
        };

        public static IEnumerable<TestCaseData> Build => Restore;

        public static IEnumerable<TestCaseData> Publish => Restore;

        public static IEnumerable<TestCaseData> Run = Restore.Where(d => ((Template)d.Arguments[0]).Type == TemplateType.WebApplication);

        public static IEnumerable<TestCaseData> Exec => Run;
    }
}
