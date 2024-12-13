// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using BasicTestApp.PropertyInjection;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class PropertyInjectionTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public PropertyInjectionTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<PropertyInjectionComponent>();
    }

    [Fact]
    public void PropertyInjection_Works_ForKeyedServices()
    {
        Browser.Equal("value-1", () => Browser.FindElement(By.Id("keyed-service-value-1")).Text);
        Browser.Equal("value-2", () => Browser.FindElement(By.Id("keyed-service-value-2")).Text);
    }

    [Fact]
    public void PropertyInjection_Throws_WhenServiceKeyIsInvalid()
    {
        Browser.Equal("value-1", () => Browser.FindElement(By.Id("keyed-service-value-1")).Text);
        Browser.Equal("value-2", () => Browser.FindElement(By.Id("keyed-service-value-2")).Text);

        Browser.Click(By.Id("invalid-service-key"));

        Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void PropertyInjection_Throws_WhenKeyedServiceTypeDoesNotMatch()
    {
        Browser.Equal("value-1", () => Browser.FindElement(By.Id("keyed-service-value-1")).Text);
        Browser.Equal("value-2", () => Browser.FindElement(By.Id("keyed-service-value-2")).Text);

        Browser.Click(By.Id("invalid-keyed-service-type"));

        Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"), TimeSpan.FromSeconds(10));
    }
}
