using System.Runtime.CompilerServices;
using Xunit;

namespace ProjectTestRunner.Helpers
{
    public class PrettyTheoryAttribute : TheoryAttribute
    {
        public PrettyTheoryAttribute([CallerMemberName] string memberName = null)
        {
            DisplayName = memberName;
        }
    }
}
