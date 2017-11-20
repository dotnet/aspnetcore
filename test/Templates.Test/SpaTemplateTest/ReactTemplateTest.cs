using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class ReactTemplateTest : SpaTemplateTestBase
    {
        public ReactTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ReactTemplate_Works_NetCore()
            => SpaTemplateImpl(null, "react");
    }
}
