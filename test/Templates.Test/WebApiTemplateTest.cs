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
