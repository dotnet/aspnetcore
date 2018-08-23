// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

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
        }
    }
}
