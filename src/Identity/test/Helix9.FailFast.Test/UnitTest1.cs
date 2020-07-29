using System;
using Xunit;

namespace XUnitTestProject9
{
    public class UnitTest1
    {
        [Fact]
        public void PassingTest()
        {

        }

        [Fact]
        public void FailFast()
        {
            Environment.FailFast("Oh noes");
        }
    }
}
