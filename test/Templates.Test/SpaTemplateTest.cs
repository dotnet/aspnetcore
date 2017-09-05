using Xunit;

namespace Templates.Test
{
    public class SpaTemplateTest : TemplateTestBase
    {
        [Theory]
        [InlineData(null, "angular")]
        [InlineData(null, "react")]
        [InlineData(null, "reactredux")]
        [InlineData(null, "aurelia")]
        [InlineData(null, "knockout")]
        [InlineData(null, "vue")]
        // Just use 'angular' as representative for .NET 4.6.1 coverage, as
        // the client-side code isn't affected by the .NET runtime choice
        [InlineData("net461", "angular")]
        public void SpaTemplate_Works(string targetFrameworkOverride, string template)
        {
            RunDotNetNew(template, targetFrameworkOverride);
            RunNpmInstall();

            using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride))
            {
                aspNetProcess.AssertOk("/");
            }
        }
    }
}
