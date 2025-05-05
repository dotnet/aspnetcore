// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.DotNet.HotReload;

internal sealed class AgentReporter
{
    private readonly List<(string message, AgentMessageSeverity severity)> _log = [];

    public void Report(string message, AgentMessageSeverity severity)
    {
        _log.Add((message, severity));
    }

    public IReadOnlyCollection<(string message, AgentMessageSeverity severity)> GetAndClearLogEntries(ResponseLoggingLevel level)
    {
        lock (_log)
        {
            var filteredLog = (level != ResponseLoggingLevel.Verbose)
                ? _log.Where(static entry => entry.severity != AgentMessageSeverity.Verbose)
                : _log;

            var log = filteredLog.ToArray();
            _log.Clear();
            return log;
        }
    }
}
