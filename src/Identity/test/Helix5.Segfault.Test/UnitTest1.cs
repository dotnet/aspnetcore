using System;
using Xunit;

namespace XUnitTestProject5
{
    public class UnitTest1
    {
        [Fact]
        public void Segfault()
        {
            unsafe
            {
                *(int*)0x12345678 = 0x1;
            }
        }
    }
}
