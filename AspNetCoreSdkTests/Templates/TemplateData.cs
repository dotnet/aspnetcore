using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public static class TemplateData
    {
        private static IEnumerable<Template> Templates { get; } = new Template[]
        {
            ConsoleApplicationTemplate.Instance,
            ClassLibraryTemplate.Instance,
            WebTemplate.Instance,
            MvcTemplate.Instance,
            RazorTemplate.Instance,
            AngularTemplate.Instance,
            ReactTemplate.Instance,
            ReactReduxTemplate.Instance,
            RazorClassLibraryTemplate.Instance,
            WebApiTemplate.Instance,
        };

        private static IEnumerable<NuGetConfig> NuGetConfigs { get; } = Enum.GetValues(typeof(NuGetConfig)).Cast<NuGetConfig>();

        private static IEnumerable<TestCaseData> All { get; } =
            from t in Templates
            from c in NuGetConfigs
            // Exclude the DotNetCore NuGet.config scenarios unless temporarily required to make tests pass
            where c != NuGetConfig.DotNetCore
            select new TestCaseData(t, c);

        private static IEnumerable<TestCaseData> IgnoreRazorClassLibEmpty { get; } =
            from d in All
            select (
                ((Template)d.Arguments[0] == RazorClassLibraryTemplate.Instance && (NuGetConfig)d.Arguments[1] == NuGetConfig.Empty) ?
                d.Ignore("https://github.com/aspnet/Universe/issues/1123") :
                d);

        public static IEnumerable<TestCaseData> Current => IgnoreRazorClassLibEmpty;

        public static IEnumerable<TestCaseData> CurrentWebApplications { get; } =
            from d in Current
            where ((Template)d.Arguments[0]).Type == TemplateType.WebApplication
            select d;

    }
}
