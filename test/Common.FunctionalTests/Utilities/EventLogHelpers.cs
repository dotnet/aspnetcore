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
            Assert.Contains(testSink.Writes, context => eventLogRegex.IsMatch(context.Message));
        }
    }
}
