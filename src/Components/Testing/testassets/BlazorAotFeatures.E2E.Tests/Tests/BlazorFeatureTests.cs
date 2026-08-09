// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorAotFeatures.E2E.Tests.Fixtures;
using BlazorServerAotSample;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlazorAotFeatures.E2E.Tests.Tests;

/// <summary>
/// One test per AOT-sensitive Blazor Server feature, asserted against whatever build of the app the
/// harness launched — including the Native AOT one, where none of these paths may use reflection.
/// </summary>
/// <remarks>
/// Each test interacts with the live circuit rather than only reading prerendered markup. A page
/// that renders proves the component was activated; only an interaction proves the generated
/// parameter setters, event bindings, JS interop contracts and JSON resolvers are correct.
/// </remarks>
[UITest]
public partial class BlazorFeatureTests : BrowserTest
{
    private ServerInstance _server = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();

        _server = await StartServerAsync<FeatureApp>(TestRoot.Servers, FeatureAppServer.Configure);

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
        _page.Console += (_, message) => TestContext.WriteLine($"CONSOLE {message.Type}: {message.Text}");
        _page.PageError += (_, error) => TestContext.WriteLine($"PAGEERROR: {error}");
        _page.WebSocket += (_, ws) =>
        {
            TestContext.WriteLine($"WS OPEN: {ws.Url}");
            ws.SocketError += (_, error) => TestContext.WriteLine($"WS ERROR: {error}");
            ws.Close += (_, _) => TestContext.WriteLine("WS CLOSE");
        };
    }

    [TestMethod]
    public async Task EventCallback_ClickRerendersWithNewState()
    {
        await GotoAsync("/counter", "#increment");

        await Expect(_page.Locator("#count")).ToHaveTextAsync("0");

        await _page.ClickAsync("#increment");
        await Expect(_page.Locator("#count")).ToHaveTextAsync("1");

        await _page.ClickAsync("#increment");
        await Expect(_page.Locator("#count")).ToHaveTextAsync("2");

        await AssertNoErrorsAsync();
    }

    [TestMethod]
    public async Task Parameters_CascadingValuesAndInjectionFlowToChild()
    {
        await GotoAsync("/parameters", "#child-bump");

        // [Parameter], [CascadingParameter] and [Inject] are all assigned through generated
        // descriptors; each one is read back from the child's rendered output.
        await Expect(_page.Locator("#child-title")).ToHaveTextAsync("First");
        await Expect(_page.Locator("#child-theme")).ToHaveTextAsync("aot-theme");
        await Expect(_page.Locator("#child-greeting")).ToHaveTextAsync("injected-greeting");
        await Expect(_page.Locator("#child-count")).ToHaveTextAsync("0");

        // EventCallback travelling child -> parent, then the new parameter value back down.
        await _page.ClickAsync("#child-bump");
        await Expect(_page.Locator("#child-count")).ToHaveTextAsync("1");

        await AssertNoErrorsAsync();
    }

    [TestMethod]
    public async Task Binding_TypedValueFlowsThroughBindExpression()
    {
        await GotoAsync("/databinding", "#name");

        await _page.FillAsync("#name", "grace");
        // @bind writes back on change, which blur triggers.
        await _page.Locator("#name").BlurAsync();

        await Expect(_page.Locator("#bound")).ToHaveTextAsync("grace");
        await AssertNoErrorsAsync();
    }

    [TestMethod]
    public async Task Forms_ValidationBlocksSubmitThenValidValueSubmits()
    {
        await GotoAsync("/forms", "#email");

        // DataAnnotationsValidator resolves the FieldIdentifier by walking generated descriptors,
        // so an invalid value must still produce a validation message and suppress OnValidSubmit.
        await _page.FillAsync("#email", "not-an-email");
        await _page.ClickAsync("#submit");
        await Expect(_page.Locator(".validation-message")).ToBeVisibleAsync();
        await Expect(_page.Locator("#status")).ToHaveTextAsync("(unsubmitted)");

        await _page.FillAsync("#email", "ada@example.com");
        await _page.ClickAsync("#submit");
        await Expect(_page.Locator("#status")).ToHaveTextAsync("valid:ada@example.com");

        await AssertNoErrorsAsync();
    }

    [TestMethod]
    public async Task JSInterop_RoundTripsBothDirections()
    {
        await GotoAsync("/jsinterop", "#prompt");

        await _page.ClickAsync("#prompt");

        // Left of the pipe is the outgoing call's return value; right of it is the result of JS
        // calling back into [JSInvokable] through a DotNetObjectReference.
        await Expect(_page.Locator("#msg")).ToHaveTextAsync("hello from js 2 | echo:hi");
        await AssertNoErrorsAsync();
    }

    [TestMethod]
    public async Task Templates_GenericComponentRendersFragmentsAndReordersByKey()
    {
        await GotoAsync("/templates", "#reverse");

        // RenderFragment<T> applied by a @typeparam component, closed over two different types.
        await Expect(_page.Locator("#names li")).ToHaveTextAsync(["ALPHA", "BETA", "GAMMA"]);
        await Expect(_page.Locator("#numbers li")).ToHaveTextAsync(["#2", "#4", "#6"]);

        await _page.ClickAsync("#reverse");
        await Expect(_page.Locator("#names li")).ToHaveTextAsync(["GAMMA", "BETA", "ALPHA"]);

        await AssertNoErrorsAsync();
    }

    [TestMethod]
    public async Task QueryParameters_ParseIntoTypedProperties()
    {
        await GotoAsync("/query?term=aot&page=7&flag=a&flag=b");

        await Expect(_page.Locator("#term")).ToHaveTextAsync("aot");
        await Expect(_page.Locator("#page")).ToHaveTextAsync("7");
        await Expect(_page.Locator("#flags")).ToHaveTextAsync("a,b");

        await AssertNoErrorsAsync();
    }

    [TestMethod]
    public async Task Virtualize_RendersMoreRowsAfterScrolling()
    {
        await GotoAsync("/virtualize");

        await Expect(_page.Locator("text=row-0")).ToBeVisibleAsync();
        await Expect(_page.Locator("text=row-400")).ToHaveCountAsync(0);

        // Scrolling drives OnSpacerBeforeVisible, whose float argument must be present in the
        // generated JS interop contracts or no further rows are ever requested.
        await _page.EvalOnSelectorAsync("#viewport", "e => e.scrollTop = 8000");

        await Expect(_page.Locator("text=row-400")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await AssertNoErrorsAsync();
    }

    [TestMethod]
    public async Task ProtectedBrowserStorage_RoundTripsAValue()
    {
        await GotoAsync("/storage", "#save");

        await _page.ClickAsync("#save");
        await _page.ClickAsync("#load");

        await Expect(_page.Locator("#loaded")).ToHaveTextAsync("ada:36");
        await AssertNoErrorsAsync();
    }

    [TestMethod]
    public async Task ProtectedBrowserStorage_RoundTripsAValueThroughACustomSerializer()
    {
        await GotoAsync("/storage", "#save-custom");

        await _page.ClickAsync("#save-custom");
        await _page.ClickAsync("#load-custom");

        // Theme has no JSON contract, so this only passes if the registered
        // ProtectedBrowserStorageSerializer<Theme> was resolved for both directions.
        await Expect(_page.Locator("#loaded-custom")).ToHaveTextAsync("solarized");
        await AssertNoErrorsAsync();
    }

    [TestMethod]
    public async Task PersistentState_SurvivesPrerenderToCircuit()
    {
        await _page.GotoAsync(_server.TestUrl + "/persistence");

        // The prerendered markup carries the freshly created token.
        var prerenderedToken = await _page.Locator("#token").TextContentAsync();
        Assert.IsFalse(string.IsNullOrEmpty(prerenderedToken));

        await _page.WaitForBlazorAsync();

        // Once the circuit takes over, the same token must come back: a new one means the state was
        // not restored and the component simply re-created it.
        await Expect(_page.Locator("#phase")).ToHaveTextAsync("circuit (restored)");
        await Expect(_page.Locator("#token")).ToHaveTextAsync(prerenderedToken!);

        await AssertNoErrorsAsync();
    }

    private async Task GotoAsync(string path, string? interactiveSelector = null)
    {
        await _page.GotoAsync(_server.TestUrl + path);

        // Every page here is prerendered and then made interactive, so assertions about behaviour
        // have to wait for the circuit rather than reading the static markup. The Blazor global
        // appears as soon as the script loads, which is before the circuit attaches its event
        // handlers, so a page that is driven by interaction also waits for a handler to land —
        // otherwise the first click lands on static markup and is silently dropped.
        await _page.WaitForBlazorAsync();

        if (interactiveSelector is not null)
        {
            await _page.WaitForInteractiveAsync(interactiveSelector);
        }
    }

    private async Task AssertNoErrorsAsync()
    {
        // #blazor-error-ui is always present in the document; it becomes visible on an unhandled
        // circuit error, which is how a missing AOT contract surfaces at runtime.
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
