using Xunit;

namespace Templates.Test
{
    public class WebApiTemplateTest : TemplateTestBase
    {
        [Theory]
        [InlineData(null)]
        [InlineData("net461")]
        public void WebApiTemplate_Works(string targetFrameworkOverride)
        {
            RunDotNetNew("api", targetFrameworkOverride);

            using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride))
            {
                aspNetProcess.AssertOk("/api/values");
                aspNetProcess.AssertNotFound("/");
            }
        }
    }
}
