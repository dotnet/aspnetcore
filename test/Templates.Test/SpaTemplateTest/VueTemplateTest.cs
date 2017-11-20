using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class VueTemplateTest : SpaTemplateTestBase
    {
        public VueTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void VueTemplate_Works_NetCore()
            => SpaTemplateImpl(null, "vue");
    }
}
