using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.DeveloperCertificates.Tools
{
    internal class Diagnostics : IDiagnostics
    {
        private readonly IReporter _reporter;

        public Diagnostics(IReporter reporter)
        {
            _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public void Debug(string message)
        {
            _reporter.Verbose(message);
        }

        public void Debug(IEnumerable<string> messages)
        {
            foreach (var message in messages)
            {
                Debug(message);
            }
        }

        public void Warn(string message)
        {
            _reporter.Warn(message);
        }

        public void Error(string message, Exception exception)
        {
            _reporter.Error(message);
            while (exception != null)
            {
                _reporter.Error("Exception message: " + exception.Message);
                exception = exception.InnerException;
            }
        }
    }
}
