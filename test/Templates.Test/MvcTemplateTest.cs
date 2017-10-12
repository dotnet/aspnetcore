using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class MvcTemplateTest : TemplateTestBase
    {
        public MvcTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(/* netcoreapp */ null, /* C# */ null)]
        [InlineData("net461", /* C# */ null)]
        [InlineData(/* netcoreapp */ null, "F#")]
        [InlineData("net461", "F#")]
        public void MvcTemplate_NoAuth_Works(string targetFrameworkOverride, string languageOverride)
        {
            RunDotNetNew("mvc", targetFrameworkOverride, language: languageOverride);

            AssertDirectoryExists("Areas", false);
            AssertDirectoryExists("Extensions", false);
            AssertFileExists("urlRewrite.config", false);
            AssertFileExists("Controllers/AccountController.cs", false);

            var projectExtension = languageOverride == "F#" ? "fsproj" : "csproj";
            var projectFileContents = ReadFile($"{ProjectName}.{projectExtension}");
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
                    aspNetProcess.AssertOk("/Home/About");
                    aspNetProcess.AssertOk("/Home/Contact");
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("net461")]
        public void MvcTemplate_IndividualAuth_Works(string targetFrameworkOverride)
        {
            RunDotNetNew("mvc", targetFrameworkOverride, auth: "Individual");
            
            AssertDirectoryExists("Extensions", false);
            AssertFileExists("urlRewrite.config", false);
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
                    aspNetProcess.AssertOk("/Home/About");
                    aspNetProcess.AssertOk("/Home/Contact");
                }
            }
        }
    }
}
