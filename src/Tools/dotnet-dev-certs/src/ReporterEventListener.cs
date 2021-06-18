using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.DeveloperCertificates.Tools
{
    internal class ReporterEventListener : EventListener
    {
        private readonly IReporter _reporter;

        public ReporterEventListener(IReporter reporter)
        {
            _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Action<string> report = eventData.Level switch
            {
                EventLevel.LogAlways => _reporter.Output,
                EventLevel.Critical => _reporter.Error,
                EventLevel.Error => _reporter.Error,
                EventLevel.Warning => _reporter.Warn,
                EventLevel.Informational => _reporter.Output,
                EventLevel.Verbose => _reporter.Verbose,
                _ => throw new ArgumentOutOfRangeException(nameof(eventData), eventData.Level, $"The value of argument '{nameof(eventData.Level)}' ({eventData.Level}) is invalid for Enum type '{nameof(EventLevel)}'.")
            };
            var message = string.Format(CultureInfo.InvariantCulture, eventData.Message ?? "", eventData.Payload?.ToArray() ?? Array.Empty<object>());
            report($"[{eventData.EventId}] " + message);
        }
    }
}
