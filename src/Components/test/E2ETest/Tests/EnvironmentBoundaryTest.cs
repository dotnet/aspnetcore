// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class EnvironmentBoundaryTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public EnvironmentBoundaryTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<EnvironmentBoundaryContainer>();
    }

    private string GetCurrentEnvironment()
    {
        var currentEnvElement = Browser.Exists(By.Id("current-environment"));
        var text = currentEnvElement.Text;
        // Text is "Current Environment: <EnvironmentName>"
        return text.Replace("Current Environment:", "").Trim();
    }

    [Fact]
    public void RendersContentWhenEnvironmentMatches()
    {
        var container = Browser.Exists(By.Id("environment-boundary-test"));
        var currentEnvironment = GetCurrentEnvironment();

        if (currentEnvironment == "Development")
        {
            // Verify Development-specific content is visible
            var devContent = container.FindElement(By.Id("dev-only-content"));
            Assert.Equal("This content is only visible in Development.", devContent.Text);

            // Verify non-production content is visible (we're in Development)
            var nonProdContent = container.FindElement(By.Id("non-production-content"));
            Assert.Equal("This content is visible in all environments except Production.", nonProdContent.Text);

            // Verify Development+Staging content is visible
            var devStagingContent = container.FindElement(By.Id("dev-staging-content"));
            Assert.Equal("This content is visible in Development and Staging.", devStagingContent.Text);
        }
        else if (currentEnvironment == "Production")
        {
            // Verify Production-specific content is visible
            var prodContent = container.FindElement(By.Id("prod-only-content"));
            Assert.Equal("This content is only visible in Production.", prodContent.Text);

            // Verify non-Development content is visible (we're in Production)
            var nonDevContent = container.FindElement(By.Id("non-dev-content"));
            Assert.Equal("This content is visible in all environments except Development.", nonDevContent.Text);
        }
        else
        {
            Assert.Fail($"Unexpected environment: {currentEnvironment}. Test supports Development and Production.");
        }
    }

    [Fact]
    public void HidesContentWhenEnvironmentDoesNotMatch()
    {
        Browser.Exists(By.Id("environment-boundary-test"));
        var currentEnvironment = GetCurrentEnvironment();

        if (currentEnvironment == "Development")
        {
            // Production-only content should not be visible in Development
            Browser.DoesNotExist(By.Id("prod-only-content"));

            // Content excluded from Development should not be visible
            Browser.DoesNotExist(By.Id("non-dev-content"));
        }
        else if (currentEnvironment == "Production")
        {
            // Development-only content should not be visible in Production
            Browser.DoesNotExist(By.Id("dev-only-content"));

            // Development+Staging content should not be visible in Production
            Browser.DoesNotExist(By.Id("dev-staging-content"));

            // Content excluded from Production should not be visible
            Browser.DoesNotExist(By.Id("non-production-content"));
        }
        else
        {
            Assert.Fail($"Unexpected environment: {currentEnvironment}. Test supports Development and Production.");
        }
    }

    [Fact]
    public void DisplaysCurrentEnvironment()
    {
        // Verify the environment is displayed correctly (should be a known environment)
        var currentEnvironment = GetCurrentEnvironment();
        Assert.True(
            currentEnvironment == "Development" || currentEnvironment == "Production" || currentEnvironment == "Staging",
            $"Expected a known environment (Development, Production, or Staging) but got: {currentEnvironment}");
    }
}
