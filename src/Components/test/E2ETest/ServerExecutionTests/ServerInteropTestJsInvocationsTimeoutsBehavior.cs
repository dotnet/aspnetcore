// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class ServerInteropTestJsInvocationsTimeoutsBehavior : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
    {
        public ServerInteropTestJsInvocationsTimeoutsBehavior(
            BrowserFixture browserFixture,
            BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: true);
            Browser.MountTestComponent<LongRunningInterop>();
        }

        [Fact]
        public async Task LongRunningJavaScriptFunctionsResultInCancellationAndWorkingAppAfterFunctionCompletion()
        {
            // Act & Assert
            var interopButton = Browser.FindElement(By.Id("btn-interop"));
            interopButton.Click();

            Browser.Exists(By.Id("done-with-interop"));

            Browser.Exists(By.Id("task-was-cancelled"));

            // wait 10 seconds, js method completes in 5 seconds, after this point it would have triggered a completion for sure.
            await Task.Delay(10000);

            var circuitFunctional = Browser.FindElement(By.Id("circuit-functional"));
            circuitFunctional.Click();

            Browser.Exists(By.Id("done-circuit-functional"));
        }
    }
}
