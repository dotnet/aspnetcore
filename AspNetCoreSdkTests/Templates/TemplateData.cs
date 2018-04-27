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
            ClassLibraryTemplate.Instance,
            ConsoleApplicationTemplate.Instance,
            WebTemplate.Instance,
            RazorTemplate.Instance,
            MvcTemplate.Instance,
            WebApiTemplate.Instance,
            RazorClassLibraryTemplate.Instance,
        };

        private static IEnumerable<NuGetConfig> NuGetConfigs { get; } = Enum.GetValues(typeof(NuGetConfig)).Cast<NuGetConfig>();

        private static IEnumerable<TestCaseData> All { get; } =
            from t in Templates
            from c in NuGetConfigs
            select new TestCaseData(t, c);

        private static IEnumerable<TestCaseData> IgnoreRazorClassLibEmpty { get; } =
            from d in All
            select (
                ((Template)d.Arguments[0] == RazorClassLibraryTemplate.Instance && (NuGetConfig)d.Arguments[1] == NuGetConfig.Empty) ?
                d.Ignore("https://github.com/aspnet/Universe/issues/1123") :
                d);

        public static IEnumerable<TestCaseData> Current => IgnoreRazorClassLibEmpty;
    }
}
