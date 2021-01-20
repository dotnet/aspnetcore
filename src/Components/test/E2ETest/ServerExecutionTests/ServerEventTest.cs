// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Tests;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class ServerEventTest : EventTest
    {
        public ServerEventTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }

        [Fact]
        public override void EventDuringBatchRendering_CanTriggerDOMEvents()
        {
            base.EventDuringBatchRendering_CanTriggerDOMEvents();
        }
    }
}
