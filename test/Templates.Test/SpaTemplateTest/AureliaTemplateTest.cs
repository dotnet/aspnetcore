using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class AureliaTemplateTest : SpaTemplateTestBase
    {
        public AureliaTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void AureliaTemplate_Works_NetCore()
            => SpaTemplateImpl(null, "aurelia");
    }
}
