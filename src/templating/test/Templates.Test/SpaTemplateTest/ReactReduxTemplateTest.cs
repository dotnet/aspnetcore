using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class ReactReduxTemplateTest : SpaTemplateTestBase
    {
        public ReactReduxTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ReactReduxTemplate_Works_NetCore()
            => SpaTemplateImpl(null, "reactredux");
    }
}
