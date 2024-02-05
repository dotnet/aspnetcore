// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Endpoints.Forms;
using Microsoft.AspNetCore.Components.Endpoints.Tests.TestComponents;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Moq;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RazorComponentResultTest
{
    [Fact]
    public void RejectsNullParameters()
    {
        Assert.Throws<ArgumentNullException>(() => new RazorComponentResult(typeof(SimpleComponent), (object)null));
        Assert.Throws<ArgumentNullException>(() => new RazorComponentResult(typeof(SimpleComponent), null));
    }

    [Fact]
    public void AcceptsDictionaryParameters()
    {
        var paramsDict = new Dictionary<string, object> { { "First", 123 } };
        var result = new RazorComponentResult(typeof(SimpleComponent), paramsDict);
        Assert.Equal(1, result.Parameters.Count);
        Assert.Equal(123, result.Parameters["First"]);
        Assert.Same(paramsDict, result.Parameters);
    }

    [Fact]
    public void AcceptsObjectParameters()
    {
        var result = new RazorComponentResult(typeof(SimpleComponent), new { Param1 = 123, Param2 = "Another" });
        Assert.Equal(2, result.Parameters.Count);
        Assert.Equal(123, result.Parameters["Param1"]);
        Assert.Equal("Another", result.Parameters["Param2"]);
    }

    [Fact]
    public async Task CanRenderComponentStatically()
    {
        // Arrange
        var result = new RazorComponentResult<SimpleComponent>();
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", GetStringContent(responseBody));
        Assert.Equal("text/html; charset=utf-8", httpContext.Response.ContentType);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ResponseIncludesStatusCodeAndContentTypeAndHtml()
    {
        // Arrange
        var result = new RazorComponentResult<SimpleComponent>
        {
            StatusCode = 123,
            ContentType = "application/test-content-type",
        };
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", GetStringContent(responseBody));
        Assert.Equal("application/test-content-type", httpContext.Response.ContentType);
        Assert.Equal(123, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task PerformsStreamingRendering()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act/Assert 1: Emits the initial pre-quiescent output to the response
        var result = new RazorComponentResult(typeof(StreamingAsyncLoadingComponent),
            PropertyHelper.ObjectToDictionary(new { LoadingTask = tcs.Task }).AsReadOnly());
        var completionTask = result.ExecuteAsync(httpContext);
        await WaitForContentWrittenAsync(responseBody);
        Assert.Equal(
            "<!--bl:X-->Loading task status: WaitingForActivation<!--/bl:X-->",
            MaskComponentIds(GetStringContent(responseBody)));

        // Assert 2: Result task remains incomplete for as long as the component's loading operation remains in flight
        // This keeps the HTTP response open
        await Task.Yield();
        Assert.False(completionTask.IsCompleted);

        // Act/Assert 3: When loading completes, it emits a streaming batch update and completes the response
        tcs.SetResult();
        await completionTask;
        Assert.Equal(
            "<!--bl:X-->Loading task status: WaitingForActivation<!--/bl:X--><blazor-ssr><template blazor-component-id=\"X\">Loading task status: RanToCompletion</template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>",
            MaskComponentIds(GetStringContent(responseBody)));
    }

    [Fact]
    public async Task EmitsEachComponentOnlyOncePerStreamingUpdate_WhenAComponentRendersTwice()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act/Assert 1: Emits the initial pre-quiescent output to the response
        var result = new RazorComponentResult(typeof(DoubleRenderingStreamingAsyncComponent),
            PropertyHelper.ObjectToDictionary(new { WaitFor = tcs.Task }).AsReadOnly());
        var completionTask = result.ExecuteAsync(httpContext);
        await WaitForContentWrittenAsync(responseBody);
        Assert.Equal(
            "<!--bl:X-->Loading...<!--/bl:X-->",
            MaskComponentIds(GetStringContent(responseBody)));

        // Act/Assert 2: When loading completes, it emits a streaming batch update with only one copy of the final output,
        // despite the RenderBatch containing two diffs from the component
        tcs.SetResult();
        await completionTask;
        Assert.Equal(
            "<!--bl:X-->Loading...<!--/bl:X--><blazor-ssr><template blazor-component-id=\"X\">Loaded</template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>",
            MaskComponentIds(GetStringContent(responseBody)));
    }

    [Fact]
    public async Task EmitsEachComponentOnlyOncePerStreamingUpdate_WhenAnAncestorAlsoUpdated()
    {
        // Since the HTML rendered for each component also includes all its descendants, we don't
        // want to render output for any component that also has an ancestor in the set of updates
        // (as it would then be output twice)

        // Arrange
        var tcs = new TaskCompletionSource();
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act/Assert 1: Emits the initial pre-quiescent output to the response
        var result = new RazorComponentResult(typeof(StreamingComponentWithChild),
            PropertyHelper.ObjectToDictionary(new { LoadingTask = tcs.Task }).AsReadOnly());
        var completionTask = result.ExecuteAsync(httpContext);
        await WaitForContentWrittenAsync(responseBody);
        var expectedInitialHtml = "<!--bl:X-->[LoadingTask: WaitingForActivation]\n<!--bl:X-->[Child render: 1]\n<!--/bl:X--><!--/bl:X-->";
        Assert.Equal(
            expectedInitialHtml,
            MaskComponentIds(GetStringContent(responseBody)));
        
        // Act/Assert 2: When loading completes, it emits a streaming batch update in which the
        // child is present only within the parent markup, not as a separate entry
        tcs.SetResult();
        await completionTask;
        Assert.Equal(
            $"{expectedInitialHtml}<blazor-ssr><template blazor-component-id=\"X\">[LoadingTask: RanToCompletion]\n<!--bl:X-->[Child render: 2]\n<!--/bl:X--></template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>",
            MaskComponentIds(GetStringContent(responseBody)));
    }

    [Fact]
    public async Task WaitsForQuiescenceIfPreventStreamingRenderingIsTrue()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act/Assert: Doesn't complete until loading finishes
        var result = new RazorComponentResult(typeof(StreamingAsyncLoadingComponent),
            PropertyHelper.ObjectToDictionary(new { LoadingTask = tcs.Task }).AsReadOnly())
        {
            PreventStreamingRendering = true
        };
        var completionTask = result.ExecuteAsync(httpContext);
        await Task.Yield();
        Assert.False(completionTask.IsCompleted);

        // Act/Assert: Does complete when loading finishes
        tcs.SetResult();
        await completionTask;
        Assert.Equal(
            "<!--bl:X-->Loading task status: RanToCompletion<!--/bl:X-->",
            MaskComponentIds(GetStringContent(responseBody)));
    }

    [Fact]
    public async Task SupportsLayouts()
    {
        // Arrange
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act
        await new RazorComponentResult(typeof(ComponentWithLayout)).ExecuteAsync(httpContext);

        // Assert
        Assert.Equal($"[TestParentLayout with content: [TestLayout with content: Page\n]\n]\n", GetStringContent(responseBody));
    }

    [Fact]
    public async Task OnNavigationBeforeResponseStarted_Redirects()
    {
        // Arrange
        var httpContext = GetTestHttpContext();

        // Act
        await new RazorComponentResult(typeof(ComponentThatRedirectsSynchronously)).ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("https://test/somewhere/else", httpContext.Response.Headers.Location);
    }

    [Fact]
    public async Task OnNavigationAfterResponseStarted_WithStreamingOff_Throws()
    {
        // Arrange
        var httpContext = GetTestHttpContext();
        var responseMock = new Mock<IHttpResponseFeature>();
        responseMock.Setup(r => r.HasStarted).Returns(true);
        responseMock.Setup(r => r.Headers).Returns(new HeaderDictionary());
        httpContext.Features.Set(responseMock.Object);

        // Act
        var result = new RazorComponentResult(typeof(StreamingComponentThatRedirectsAsynchronously))
        {
            PreventStreamingRendering = true
        };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => result.ExecuteAsync(httpContext));

        // Assert
        Assert.Contains("A navigation command was attempted during prerendering after the server already started sending the response", ex.Message);
    }

    [Fact]
    public async Task OnNavigationAfterResponseStarted_WithStreamingOn_EmitsCommand()
    {
        // Arrange
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act
        await new RazorComponentResult(typeof(StreamingComponentThatRedirectsAsynchronously)).ExecuteAsync(httpContext);

        // Assert
        var markup = MaskComponentIds(GetStringContent(responseBody));
        Assert.StartsWith(
            "<!--bl:X-->Some output\n<!--/bl:X--><blazor-ssr><template type=\"redirection\">_framework/opaque-redirect?url=",
            markup);
        Assert.EndsWith(
            "</template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>",
            markup);
    }

    [Fact]
    public async Task OnUnhandledExceptionBeforeResponseStarted_Throws()
    {
        // Arrange
        var httpContext = GetTestHttpContext();

        // Act
        var ex = await Assert.ThrowsAsync<InvalidTimeZoneException>(() =>
            new RazorComponentResult(typeof(ComponentThatThrowsSynchronously)).ExecuteAsync(httpContext));

        // Assert
        Assert.Contains("Test message", ex.Message);
    }

    [Fact]
    public async Task OnUnhandledExceptionAfterResponseStarted_WithStreamingOff_Throws()
    {
        // Arrange
        var httpContext = GetTestHttpContext();

        // Act
        var ex = await Assert.ThrowsAsync<InvalidTimeZoneException>(
            () => new RazorComponentResult(typeof(StreamingComponentThatThrowsAsynchronously)) { PreventStreamingRendering = true }
                .ExecuteAsync(httpContext));

        // Assert
        Assert.Contains("Test message", ex.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnUnhandledExceptionAfterResponseStarted_WithStreamingOn_EmitsCommand(bool isDevelopmentEnvironment)
    {
        // Arrange
        var httpContext = GetTestHttpContext(isDevelopmentEnvironment ? Environments.Development : Environments.Production);
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        var expectedResponseExceptionInfo = isDevelopmentEnvironment
            ? "System.InvalidTimeZoneException: Test message with &lt;b&gt;markup&lt;/b&gt;"
            : "There was an unhandled exception on the current request. For more details turn on detailed exceptions by setting &#x27;DetailedErrors: true&#x27; in &#x27;appSettings.Development.json&#x27;";

        // Act
        var ex = await Assert.ThrowsAsync<InvalidTimeZoneException>(
            () => new RazorComponentResult(typeof(StreamingComponentThatThrowsAsynchronously))
                .ExecuteAsync(httpContext));

        // Assert
        Assert.Contains("Test message with <b>markup</b>", ex.Message);
        Assert.Contains(
            $"<!--bl:X-->Some output\n<!--/bl:X--><blazor-ssr><template type=\"error\">{expectedResponseExceptionInfo}",
            MaskComponentIds(GetStringContent(responseBody)));
    }

    [Fact]
    public async Task StreamingRendering_IsOffByDefault_AndCanBeEnabledForSubtree()
    {
        // Arrange
        var testContext = PrepareVaryStreamingScenariosTests();
        var initialOutputTask = testContext.Renderer.NonStreamingPendingTasksCompletion;

        // Act/Assert: Even if all other blocking tasks complete, we don't produce output until the top-level
        // nonstreaming component completes
        Assert.NotNull(initialOutputTask);
        testContext.WithinNestedNonstreamingRegionTask.SetResult();
        await Task.Yield(); // Just to show it's still not completed after
        Assert.False(initialOutputTask.IsCompleted);

        // Act/Assert: Produce initial output, noting absence of streaming markers at top level
        testContext.TopLevelComponentTask.SetResult();
        await initialOutputTask;
        await WaitForContentWrittenAsync(testContext.ResponseBody);
        var html = MaskComponentIds(GetStringContent(testContext.ResponseBody));
        Assert.StartsWith("[Top level component: Loaded]", html);
        Assert.Contains("[Within streaming region: <!--bl:X-->Loading...<!--/bl:X-->]", html);
        Assert.Contains("[Within nested nonstreaming region: Loaded]", html);
        Assert.DoesNotContain("blazor-ssr", html);

        // Act/Assert: Complete the streaming
        testContext.WithinStreamingRegionTask.SetResult();
        await testContext.Quiescence;
        html = GetStringContent(testContext.ResponseBody);
        Assert.EndsWith(
            "<blazor-ssr><template blazor-component-id=\"X\">Loaded</template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>",
            MaskComponentIds(html));
    }

    [Fact]
    public async Task StreamingRendering_CanBeDisabledForSubtree()
    {
        // Arrange
        var testContext = PrepareVaryStreamingScenariosTests();
        var initialOutputTask = testContext.Renderer.NonStreamingPendingTasksCompletion;

        // Act/Assert: Even if all other nonblocking tasks complete, we don't produce output until
        // the component in the nonstreaming subtree is quiescent
        Assert.NotNull(initialOutputTask);
        testContext.TopLevelComponentTask.SetResult();
        await Task.Yield(); // Just to show it's still not completed after
        Assert.False(initialOutputTask.IsCompleted);

        // Act/Assert: Does produce output when nonstreaming subtree is quiescent
        testContext.WithinNestedNonstreamingRegionTask.SetResult();
        await initialOutputTask;
        await WaitForContentWrittenAsync(testContext.ResponseBody);
        var html = MaskComponentIds(GetStringContent(testContext.ResponseBody));
        Assert.Contains("[Within streaming region: <!--bl:X-->Loading...<!--/bl:X-->]", html);
        Assert.DoesNotContain("blazor-ssr", html);

        // Assert: No boundary markers around nonstreaming components, even if they are nested in a streaming region
        Assert.Contains("[Top level component: Loaded]", html);
        Assert.Contains("[Within nested nonstreaming region: Loaded]", html);

        // Act/Assert: Complete the streaming
        testContext.WithinStreamingRegionTask.SetResult();
        await testContext.Quiescence;
        html = GetStringContent(testContext.ResponseBody);
        Assert.EndsWith(
            "<blazor-ssr><template blazor-component-id=\"X\">Loaded</template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>",
            MaskComponentIds(html));
    }

    // We don't want these tests to be hardcoded for specific component ID numbers, so replace them all with X for assertions
    private static readonly Regex TemplateElementComponentIdRegex = new Regex("blazor-component-id=\"\\d+\"");
    private static readonly Regex OpenBoundaryMarkerRegex = new Regex("<!--bl:\\d+-->");
    private static readonly Regex CloseBoundaryMarkerRegex = new Regex("<!--/bl:\\d+-->");
    private static string MaskComponentIds(string html)
    {
        html = TemplateElementComponentIdRegex.Replace(html, "blazor-component-id=\"X\"");
        html = OpenBoundaryMarkerRegex.Replace(html, "<!--bl:X-->");
        html = CloseBoundaryMarkerRegex.Replace(html, "<!--/bl:X-->");
        return html;
    }

    private VaryStreamingScenariosContext PrepareVaryStreamingScenariosTests()
    {
        var httpContext = GetTestHttpContext();
        var renderer = httpContext.RequestServices.GetRequiredService<EndpointHtmlRenderer>();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        var topLevelComponentTask = new TaskCompletionSource();
        var withinStreamingRegionTask = new TaskCompletionSource();
        var withinNestedNonstreamingRegionTask = new TaskCompletionSource();
        var parameters = new Dictionary<string, object>
        {
            { nameof(VaryStreamingScenarios.TopLevelComponentTask), topLevelComponentTask.Task },
            { nameof(VaryStreamingScenarios.WithinStreamingRegionTask), withinStreamingRegionTask.Task },
            { nameof(VaryStreamingScenarios.WithinNestedNonstreamingRegionTask), withinNestedNonstreamingRegionTask.Task },
        };

        var result = new RazorComponentResult(typeof(VaryStreamingScenarios), parameters);
        var quiescence = result.ExecuteAsync(httpContext);

        return new(renderer, quiescence, responseBody, topLevelComponentTask, withinStreamingRegionTask, withinNestedNonstreamingRegionTask);
    }

    private record struct VaryStreamingScenariosContext(
        EndpointHtmlRenderer Renderer,
        Task Quiescence,
        MemoryStream ResponseBody,
        TaskCompletionSource TopLevelComponentTask,
        TaskCompletionSource WithinStreamingRegionTask,
        TaskCompletionSource WithinNestedNonstreamingRegionTask);

    private static string GetStringContent(MemoryStream stream)
    {
        stream.Position = 0;
        return new StreamReader(stream).ReadToEnd().Replace("\r\n", "\n");
    }

    public static DefaultHttpContext GetTestHttpContext(string environmentName = null)
    {
        var mockWebHostEnvironment = Mock.Of<IWebHostEnvironment>(
            x => x.EnvironmentName == (environmentName ?? Environments.Production));
        var serviceCollection = new ServiceCollection()
            .AddAntiforgery()
            .AddSingleton(new DiagnosticListener("test"))
            .AddSingleton<IWebHostEnvironment>(mockWebHostEnvironment)
            .AddSingleton<EndpointHtmlRenderer>()
            .AddSingleton<IComponentPrerenderer>(services => services.GetRequiredService<EndpointHtmlRenderer>())
            .AddSingleton<NavigationManager, FakeNavigationManager>()
            .AddSingleton<ServerComponentSerializer>()
            .AddSingleton<ComponentStatePersistenceManager>()
            .AddSingleton<IDataProtectionProvider, FakeDataProtectionProvider>()
            .AddSingleton<HttpContextFormDataProvider>()
            .AddSingleton<ComponentStatePersistenceManager>()
            .AddSingleton<PersistentComponentState>(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State)
            .AddSingleton<AntiforgeryStateProvider, EndpointAntiforgeryStateProvider>()
            .AddLogging();

        var result = new DefaultHttpContext { RequestServices = serviceCollection.BuildServiceProvider() };
        result.Request.Scheme = "https";
        result.Request.Host = new HostString("test");
        return result;
    }

    // Some tests want to observe the output state when some content has been written, but without waiting for
    // the whole streaming task to complete. There isn't any public API that exposes this specific phase, so
    // rather than making the renderer track additional tasks that would only be used in tests, this allows the
    // tests to continue once the first batch of output was written.
    private static async Task WaitForContentWrittenAsync(Stream stream, TimeSpan? timeout = default)
    {
        var timeoutRemaining = timeout.GetValueOrDefault(TimeSpan.FromSeconds(1));
        var pollInterval = TimeSpan.FromMilliseconds(50);
        do
        {
            if (stream.Position > 0)
            {
                return;
            }
            await Task.Delay(pollInterval);
            timeoutRemaining = timeoutRemaining - pollInterval;
        } while (timeoutRemaining.TotalMilliseconds > 0);

        Assert.Fail("Timeout elapsed without content being written");
    }

    class FakeDataProtectionProvider : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose)
            => new FakeDataProtector();

        class FakeDataProtector : IDataProtector
        {
            public IDataProtector CreateProtector(string purpose) => this;
            public byte[] Protect(byte[] plaintext) => new byte[] { 1, 2, 3 };
            public byte[] Unprotect(byte[] protectedData) => throw new NotImplementedException();
        }
    }

    class FakeNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
    {
        public new void Initialize(string baseUri, string uri)
            => base.Initialize(baseUri, uri);

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            // Equivalent to what RemoteNavigationManager would do
            var absoluteUriString = ToAbsoluteUri(uri).AbsoluteUri;
            throw new NavigationException(absoluteUriString);
        }
    }
}
