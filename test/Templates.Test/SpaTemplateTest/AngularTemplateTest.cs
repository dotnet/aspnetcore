using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class AngularTemplateTest : SpaTemplateTestBase
    {
        public AngularTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        // Just use 'angular' as representative for .NET 4.6.1 coverage, as
        // the client-side code isn't affected by the .NET runtime choice
        public void AngularTemplate_Works_NetFramework()
            => SpaTemplateImpl("net461", "angular");

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void AngularTemplate_NoHttps_Works_NetFramework()
            => SpaTemplateImpl("net461", "angular", true);

        [Fact]
        public void AngularTemplate_Works_NetCore()
            => SpaTemplateImpl(null, "angular");
    }
}
