using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class KnockoutTemplateTest : SpaTemplateTestBase
    {
        public KnockoutTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void KnockoutTemplate_Works_NetCore()
            => SpaTemplateImpl(null, "knockout");
    }
}
