using Xunit;

namespace Templates.Test
{
    public class EmptyWebTemplateTest : TemplateTestBase
    {
        [Theory]
        [InlineData(null)]
        [InlineData("net461")]
        public void EmptyWebTemplate_Works(string targetFrameworkOverride)
        {
            RunDotNetNew("web", targetFrameworkOverride);

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride, publish))
                {
                    aspNetProcess.AssertOk("/");
                }
            }
        }
    }
}
