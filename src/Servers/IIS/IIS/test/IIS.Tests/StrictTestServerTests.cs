// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    public class StrictTestServerTests: LoggedTest
    {
        public override void Dispose()
        {
            base.Dispose();
            Assert.DoesNotContain(TestSink.Writes, w => w.LogLevel > LogLevel.Information);
        }

        protected static TaskCompletionSource<bool> CreateTaskCompletionSource()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
