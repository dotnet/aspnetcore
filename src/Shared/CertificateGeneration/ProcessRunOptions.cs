using System.Collections.Generic;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal record ProcessRunOptions
    {
        public List<string> Command { get; } = new();
        public bool ThrowOnFailure { get; init; } = true;
        public bool ReadStandardOutput { get; init; }
        public bool ReadStandardError { get; init; } = true;
        public bool Elevate { get; init; }
        public bool IsInteractive { get; init; }
        public string? StandardInput { get; init; }
    }
}