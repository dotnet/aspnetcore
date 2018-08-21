// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class EventLogHelpers
    {
        public static void VerifyEventLogEvent(IISDeploymentResult deploymentResult, ITestSink testSink, string expectedRegexMatchString)
        {
            Assert.True(deploymentResult.HostProcess.HasExited);

            var builder = new StringBuilder();

            foreach (var context in testSink.Writes)
            {
                builder.Append(context.Message);
            }

            var eventLogRegex = new Regex($"Event Log: (.*?){expectedRegexMatchString}(.*?)End Event Log Message.", RegexOptions.Singleline);

            Assert.Matches(eventLogRegex, builder.ToString());
        }
    }
}
