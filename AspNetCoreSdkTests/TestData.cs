using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreSdkTests
{
    public static class TestData
    {
        public static IEnumerable<TestCaseData> AllTemplates { get; } =
            from t in Enum.GetValues(typeof(Template)).Cast<Template>()
            from c in Enum.GetValues(typeof(NuGetConfig)).Cast<NuGetConfig>()
            let data = new TestCaseData(t, c)
            select (
                c == NuGetConfig.NuGetOrg ?
                data.Ignore("RC1 not yet published to nuget.org") :
                data);

        public static IEnumerable<TestCaseData> ApplicationTemplates { get; } =
            from d in AllTemplates
            where ((Template)d.Arguments[0] != Template.RazorClassLib)
            select d;
    }
}
