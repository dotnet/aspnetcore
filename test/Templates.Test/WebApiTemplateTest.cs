using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class WebApiTemplateTest : TemplateTestBase
    {
        public WebApiTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(null)]
        [InlineData("net461")]
        public void WebApiTemplate_Works(string targetFrameworkOverride)
        {
            RunDotNetNew("webapi", targetFrameworkOverride);

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride, publish))
                {
                    aspNetProcess.AssertOk("/api/values");
                    aspNetProcess.AssertNotFound("/");
                }
            }
        }
    }
}
