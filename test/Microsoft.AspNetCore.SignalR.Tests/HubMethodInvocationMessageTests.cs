// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.SignalR.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class HubMethodInvocationMessageTests
    {
        [Fact]
        public void InvocationMessageToStringPutsErrorInArgs()
        {
            var hubMessage = new InvocationMessage("echo", ExceptionDispatchInfo.Capture(new Exception("test")), null);

            Assert.Equal("InvocationMessage { InvocationId: \"\", Target: \"echo\", Arguments: [ Error: test ] }", hubMessage.ToString());
        }

        [Fact]
        public void StreamInvocationMessageToStringPutsErrorInArgs()
        {
            var hubMessage = new StreamInvocationMessage("3", "echo", ExceptionDispatchInfo.Capture(new Exception("test")), null);

            Assert.Equal("StreamInvocation { InvocationId: \"3\", Target: \"echo\", Arguments: [ Error: test ] }", hubMessage.ToString());
        }
    }
}
