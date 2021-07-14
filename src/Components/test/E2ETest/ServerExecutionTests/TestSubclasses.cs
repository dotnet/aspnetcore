// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Tests;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class ServerBindTest : BindTest
    {
        public ServerBindTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerEventBubblingTest : EventBubblingTest
    {
        public ServerEventBubblingTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerInteropTest : InteropTest
    {
        public ServerInteropTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution().WithAdditionalArguments(GetAdditionalArguments()), output)
        {
        }

        private static string[] GetAdditionalArguments() =>
            new string[] { "--detailedErrors", "true" };
    }

    public class ServerRoutingTest : RoutingTest
    {
        public ServerRoutingTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerCascadingValueTest : CascadingValueTest
    {
        public ServerCascadingValueTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerEventCallbackTest : EventCallbackTest
    {
        public ServerEventCallbackTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerFormsTest : FormsTest
    {
        public ServerFormsTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerKeyTest : KeyTest
    {
        public ServerKeyTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerInputFileTest : InputFileTest
    {
        public ServerInputFileTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerVirtualizationTest : VirtualizationTest
    {
        public ServerVirtualizationTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerDynamicComponentRenderingTest : DynamicComponentRenderingTest
    {
        public ServerDynamicComponentRenderingTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerEventCustomArgsTest : EventCustomArgsTest
    {
        public ServerEventCustomArgsTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerErrorBoundaryTest : ErrorBoundaryTest
    {
        public ServerErrorBoundaryTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerDelegateEventBindingTest : DelegateEventBindingTest
    {
        public ServerDelegateEventBindingTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }

    public class ServerTwoWayBindingTest : TwoWayBindingTest
    {
        public ServerTwoWayBindingTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }
    }
}
