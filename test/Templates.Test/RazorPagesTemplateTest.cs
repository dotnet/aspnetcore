using Xunit;

namespace Templates.Test
{
    public class RazorPagesTemplateTest : TemplateTestBase
    {
        [Theory]
        [InlineData(null)]
        [InlineData("net461")]
        public void RazorPagesTemplate_NoAuth_Works(string targetFrameworkOverride)
        {
            RunDotNetNew("razor", targetFrameworkOverride);
            
            AssertDirectoryExists("Extensions", false);
            AssertFileExists("Controllers/AccountController.cs", false);

            var projectFileContents = ReadFile($"{ProjectName}.csproj");
            Assert.DoesNotContain(".db", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
            Assert.DoesNotContain("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);
            Assert.DoesNotContain("Microsoft.Extensions.SecretManager.Tools", projectFileContents);

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride, publish))
                {
                    aspNetProcess.AssertOk("/");
                    aspNetProcess.AssertOk("/About");
                    aspNetProcess.AssertOk("/Contact");
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("net461")]
        public void RazorPagesTemplate_IndividualAuth_Works(string targetFrameworkOverride)
        {
            RunDotNetNew("razor", targetFrameworkOverride, auth: "Individual");

            AssertDirectoryExists("Extensions", true);
            AssertFileExists("Controllers/AccountController.cs", true);

            var projectFileContents = ReadFile($"{ProjectName}.csproj");
            Assert.Contains(".db", projectFileContents);
            Assert.Contains("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
            Assert.Contains("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
            Assert.Contains("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);
            Assert.Contains("Microsoft.Extensions.SecretManager.Tools", projectFileContents);

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride, publish))
                {
                    aspNetProcess.AssertOk("/");
                    aspNetProcess.AssertOk("/About");
                    aspNetProcess.AssertOk("/Contact");
                }
            }
        }
    }
}
