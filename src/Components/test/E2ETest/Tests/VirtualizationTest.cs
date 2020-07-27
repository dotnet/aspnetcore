using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests
{
    public class VirtualizationTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public VirtualizationTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
            Browser.MountTestComponent<VirtualizationComponent>();
        }

        [Fact]
        public void VirtualizeFixed_CanRenderStacked()
        {
            Browser.True(() => Browser.FindElements(By.Id("stacked-top-item")).Count > 0);
            Browser.True(() => Browser.FindElements(By.Id("stacked-bottom-item")).Count == 0);
        }
    }
}
