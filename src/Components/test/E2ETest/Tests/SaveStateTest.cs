// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

// These tests are for Blazor Server and Webassembly implementations
// For Blazor Web, check StatePersistenceTest.cs
public class SaveStateTest : ServerTestBase<AspNetSiteServerFixture>
{
    public SaveStateTest(
        BrowserFixture browserFixture,
        AspNetSiteServerFixture serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.BuildWebHostMethod = Program.BuildWebHost<SaveState>;
        serverFixture.Environment = AspNetEnvironment.Development;
    }

    protected override void InitializeAsyncCore()
    {
        Navigate("/save-state");
    }

    [Theory]
    [InlineData("SingleComponentServer")]
    [InlineData("SingleComponentClient")]
    public void PreservesStateForSingleServerComponent(string link)
    {
        Browser.Click(By.Id(link));
        var prerenderState = Browser.Exists(By.Id("state-State1")).Text;
        var serviceStateAtState1 = Browser.Exists(By.Id("service-State1")).Text;
        var serviceState = Browser.Exists(By.Id("service-state")).Text;
        var serviceStateAfter = Browser.Exists(By.Id("service-state-after")).Text;

        BeginInteractivity();

        Browser.Contains("true", () => Browser.FindElement(By.Id("restored-State1")).Text);
        var newState = Browser.Exists(By.Id("state-State1")).Text;
        var newServiceStateAtState1 = Browser.Exists(By.Id("service-State1")).Text;
        var newServiceState = Browser.Exists(By.Id("service-state")).Text;

        Assert.Equal(newState, prerenderState);

        // The value won't match to the updated value that we created after persisting the state.
        Assert.NotEqual(serviceStateAfter, newServiceState);

        // The value matches the one that was persisted on to the page.
        Assert.Equal(newServiceState, serviceState);

        // The state used upon re-render is the updated value that the service persisted (the component catches up)
        Assert.Equal(serviceState, newServiceStateAtState1);
    }

    [Theory]
    [InlineData("SingleComponentServer")]
    [InlineData("SingleComponentClient")]
    public void PreservedStateIsOnlyAvailableDuringFirstRender(string link)
    {
        Browser.Click(By.Id(link));
        var prerenderState = Browser.Exists(By.Id("state-State1")).Text;
        var extraPrerenderState = Browser.Exists(By.Id("extra-Extra")).Text;

        BeginInteractivity();

        Browser.Contains("true", () => Browser.FindElement(By.Id("restored-State1")).Text);
        var newState = Browser.Exists(By.Id("state-State1")).Text;

        Browser.DoesNotExist(By.Id("extra-Extra"));

        Browser.Click(By.Id("button-Extra"));

        var extraPrerenderStateAfterClick = Browser.Exists(By.Id("extra-Extra")).Text;
        Assert.Equal(newState, prerenderState);
        Assert.NotEqual(extraPrerenderState, extraPrerenderStateAfterClick);
    }

    [Theory]
    [InlineData("MultipleComponentServer")]
    [InlineData("MultipleComponentClient")]
    public void PreservesStateForMultipleServerComponent(string link)
    {
        Browser.Click(By.Id(link));
        var prerenderState1 = Browser.Exists(By.Id("state-State1")).Text;
        var prerenderState2 = Browser.Exists(By.Id("state-State2")).Text;
        var serviceStateAtState1 = Browser.Exists(By.Id("service-State1")).Text;
        var serviceStateAtState2 = Browser.Exists(By.Id("service-State2")).Text;
        var serviceState = Browser.Exists(By.Id("service-state")).Text;
        var serviceStateAfter = Browser.Exists(By.Id("service-state-after")).Text;

        BeginInteractivity();

        Browser.Contains("true", () => Browser.FindElement(By.Id("restored-State1")).Text);
        var newState1 = Browser.Exists(By.Id("state-State1")).Text;
        Assert.Equal(newState1, prerenderState1);

        Browser.Contains("true", () => Browser.FindElement(By.Id("restored-State2")).Text);
        var newState2 = Browser.Exists(By.Id("state-State2")).Text;
        Assert.Equal(newState2, prerenderState2);

        var newServiceStateAtState1 = Browser.Exists(By.Id("service-State1")).Text;
        var newServiceStateAtState2 = Browser.Exists(By.Id("service-State2")).Text;
        var newServiceState = Browser.Exists(By.Id("service-state")).Text;

        // These states were not equal because the state changed between the render of the two components.
        Assert.NotEqual(serviceStateAtState1, serviceStateAtState2);

        // After interactivity starts they catch up an become the same value
        Assert.Equal(newServiceStateAtState1, newServiceStateAtState2);

        // The value won't match to the updated value that we created after persisting the state.
        Assert.NotEqual(serviceStateAfter, newServiceState);

        // The value matches the one that was persisted on to the page.
        Assert.Equal(newServiceState, serviceState);

        // The state used upon re-render is the updated value that the service persisted (the component catches up)
        Assert.Equal(serviceState, newServiceStateAtState1);
    }

    private void BeginInteractivity()
    {
        Browser.Exists(By.Id("load-boot-script")).Click();
    }
}
