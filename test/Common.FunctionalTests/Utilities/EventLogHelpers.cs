// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class EventLogHelpers
    {
        public static void VerifyEventLogEvent(ITestSink testSink, string expectedRegexMatchString)
        {
            var eventLogRegex = new Regex($"Event Log: {expectedRegexMatchString}");

            int count = 0;
            foreach (var context in testSink.Writes)
            {
                if (eventLogRegex.IsMatch(context.Message))
                {
                    count++;
                }
            }
            Assert.Equal(1, count);
        }
    }
}
