using System;
using System.Diagnostics;
using System.Text;

namespace ProjectTestRunner.Helpers
{
    public class ProcessEx
    {
        private readonly Process _process;
        private readonly StringBuilder _stderr;
        private readonly StringBuilder _stdout;

        public ProcessEx(Process p)
        {
            _stdout = new StringBuilder();
            _stderr = new StringBuilder();

            _process = p;
            p.OutputDataReceived += OnOutputData;
            p.ErrorDataReceived += OnErrorData;
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
        }

        public string Error => _stderr.ToString();

        public string Output => _stdout.ToString();

        public int ExitCode => _process.ExitCode;

        public static implicit operator Process(ProcessEx self)
        {
            return self._process;
        }

        private void OnErrorData(object sender, DataReceivedEventArgs e)
        {
            _stderr.AppendLine(e.Data);
            try
            {
                Console.Error.WriteLine(e.Data);
            }
            catch
            {
            }
        }

        private void OnOutputData(object sender, DataReceivedEventArgs e)
        {
            _stdout.AppendLine(e.Data);

            try
            {
                Console.WriteLine(e.Data);
            }
            catch
            {
            }
        }

        public bool WaitForExit(int milliseconds)
        {
            return _process.WaitForExit(milliseconds);
        }

        public void WaitForExit()
        {
            _process.WaitForExit();
        }
    }
}
