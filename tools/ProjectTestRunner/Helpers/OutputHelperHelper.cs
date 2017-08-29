using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace ProjectTestRunner.Helpers
{
    internal class OutputHelperHelper : TextWriter
    {
        private ITestOutputHelper _outputHelper;

        public OutputHelperHelper(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {            
        }

        public override void WriteLine(string format, params object[] arg)
        {
            _outputHelper.WriteLine(format, arg);
        }

        public override void WriteLine(string message)
        {
            _outputHelper.WriteLine(message ?? "");
        }
    }
}