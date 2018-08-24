// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class EventLogHelpers
    {
        private static readonly Regex EventLogRegex = new Regex("Event Log: (?<EventLogMessage>.+?)End Event Log Message.", RegexOptions.Singleline | RegexOptions.Compiled);

        public static void VerifyEventLogEvent(IISDeploymentResult deploymentResult, ITestSink testSink, string expectedRegexMatchString)
        {
            Assert.True(deploymentResult.HostProcess.HasExited);

            var builder = new StringBuilder();

            foreach (var context in testSink.Writes)
            {
                builder.Append(context.Message);
            }

            var count = 0;
            var expectedRegex = new Regex(expectedRegexMatchString, RegexOptions.Singleline);
            foreach (Match match in EventLogRegex.Matches(builder.ToString()))
            {
                var eventLogText = match.Groups["EventLogMessage"].Value;
                if (expectedRegex.IsMatch(eventLogText))
                {
                    count++;
                }
            }

            Assert.True(count > 0, $"'{expectedRegexMatchString}' didn't match any event log messaged");
            Assert.True(count < 2, $"'{expectedRegexMatchString}' matched more then one event log message");

            var eventLog = new EventLog("Application");

            // Perf: only get the last 20 events from the event log.
            // Eventlog is already sorted based on time of event in ascending time.
            // Add results in reverse order.
            var expectedRegexEventLog = new Regex(expectedRegexMatchString);
            var processIdString = $"Process Id: {deploymentResult.HostProcess.Id}.";

            for (var i = eventLog.Entries.Count - 1; i >= eventLog.Entries.Count - 20; i--)
            {
                var eventLogEntry = eventLog.Entries[i];
                if (eventLogEntry.ReplacementStrings == null ||
                    eventLogEntry.ReplacementStrings.Length < 3)
                {
                    continue;
                }

                // ReplacementStings == EventData collection in EventLog
                // This is unaffected if event providers are not registered correctly
                if (eventLogEntry.Source == AncmVersionToMatch(deploymentResult) &&
                    processIdString == eventLogEntry.ReplacementStrings[1] &&
                    expectedRegex.IsMatch(eventLogEntry.ReplacementStrings[0]))
                {
                    return;
                }
            }

            Assert.True(false, $"'{expectedRegexMatchString}' didn't match any event log messaged.");
        }

        private static string AncmVersionToMatch(IISDeploymentResult deploymentResult)
        {
            return "IIS " +
                (deploymentResult.DeploymentParameters.ServerType == ServerType.IISExpress ? "Express " : "") +
                "AspNetCore Module" +
                (deploymentResult.DeploymentParameters.AncmVersion == AncmVersion.AspNetCoreModuleV2 ? " V2" : "");
        }
    }
}
