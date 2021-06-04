using System.Text;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal record ProcessRunResult
    {
        public bool IsSuccess => ExitCode == 0;
        public string CommandLine { get; init; } = "";
        public int ExitCode { get; init; }
        public StringBuilder? StandardOutput { get; init; }
        public StringBuilder? StandardError { get; init; }
    }
}