using System;
using System.Collections;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTestProject2
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void LogTestOutput()
        {
            Console.WriteLine("This is direct console output from the test");

            _testOutputHelper.WriteLine("This is line from a test!");

            _testOutputHelper.WriteLine("This is line from another line from a test!");

            foreach (DictionaryEntry pair in Environment.GetEnvironmentVariables())
            {
                _testOutputHelper.WriteLine(pair.Key + "=" + pair.Value);
            }

            Assert.True(false);
        }
    }
}
