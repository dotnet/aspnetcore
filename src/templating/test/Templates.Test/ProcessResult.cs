using System.Diagnostics;

namespace Templates.Test
{
    public readonly struct ProcessResult
    {
        public ProcessResult(ProcessStartInfo processStartInfo, int exitCode, string output)
        {
            ProcessStartInfo = processStartInfo;
            ExitCode = exitCode;
            Output = output;
        }

        public ProcessStartInfo ProcessStartInfo { get; }
        public int ExitCode { get; }
        public string Output { get; }
    }
}