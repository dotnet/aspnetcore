// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Endpoints.DependencyInjection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Endpoints.Forms;
using Microsoft.AspNetCore.Components.Sections;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Diagnostics;
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
        Assert.Single(result.Parameters);
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
    public async Task StreamingSectionErrorBoundary_AfterInitialFlush_EmitsErrorContentWithoutStreamError()
    {
        var releaseProvider = new TaskCompletionSource();
        var httpContext = GetTestHttpContext();
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "streaming-section"));
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        var result = new RazorComponentResult(
            typeof(StreamingSectionErrorBoundaryHost),
            PropertyHelper.ObjectToDictionary(new { ReleaseProvider = releaseProvider.Task }).AsReadOnly());
        var completionTask = result.ExecuteAsync(httpContext);
        await WaitForContentWrittenAsync(responseBody);

        releaseProvider.SetResult();
        await completionTask;

        var html = MaskComponentIds(GetStringContent(responseBody));
        Assert.Contains("Boundary fallback", html);
        Assert.DoesNotContain("type=\"error\"", html);
    }

    [Fact]
    public async Task SectionValidation_AwaitsAsyncErrorFallback()
    {
        var releaseFallback = new TaskCompletionSource();
        var fallbackStarted = new TaskCompletionSource();
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        var result = new RazorComponentResult(typeof(NonStreamingSectionErrorHost), new
        {
            ReleaseFallback = releaseFallback.Task,
            FallbackStarted = fallbackStarted,
        }) { PreventStreamingRendering = true };
        var completion = result.ExecuteAsync(httpContext);
        try
        {
            await fallbackStarted.Task;
            Assert.False(completion.IsCompleted);
        }
        finally
        {
            releaseFallback.SetResult();
        }
        await completion;
        Assert.Contains("Fallback loaded", GetStringContent(responseBody));
        Assert.DoesNotContain("Fallback loading", GetStringContent(responseBody));
    }

    [Fact]
    public async Task NonStreamingSectionValidation_PrecedesInitialFlushWithStreamingSibling()
    {
        var releaseStreaming = new TaskCompletionSource();
        var httpContext = GetTestHttpContext();
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "section-first-flush"));
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        var completion = new RazorComponentResult(typeof(NonStreamingSectionErrorHost), new
        {
            ReleaseStreaming = releaseStreaming.Task,
        }).ExecuteAsync(httpContext);
        try
        {
            await WaitForContentWrittenAsync(responseBody);
            Assert.False(completion.IsCompleted);
            Assert.Contains("Boundary fallback", GetStringContent(responseBody));
            Assert.DoesNotContain("Mismatched content", GetStringContent(responseBody));
        }
        finally
        {
            releaseStreaming.SetResult();
            await completion;
        }

        // A streaming outlet, unlike the non-streaming outlet above, must retain its pending validation.
        var streamingContext = GetTestHttpContext();
        streamingContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "streaming-section-control"));
        var streamingBody = new MemoryStream();
        streamingContext.Response.Body = streamingBody;
        var releaseStreamingOutlet = new TaskCompletionSource();
        var streamingCompletion = new RazorComponentResult(typeof(StreamingInitialSectionErrorHost), new
        {
            ReleaseStreaming = releaseStreamingOutlet.Task,
        }).ExecuteAsync(streamingContext);
        try
        {
            await WaitForContentWrittenAsync(streamingBody);
            Assert.Contains("Mismatched content", GetStringContent(streamingBody));
            Assert.DoesNotContain("Boundary fallback", GetStringContent(streamingBody));
        }
        finally
        {
            releaseStreamingOutlet.SetResult();
            await streamingCompletion;
        }

        Assert.Contains("Boundary fallback", GetStringContent(streamingBody));
        Assert.DoesNotContain("type=\"error\"", GetStringContent(streamingBody));
    }

    [Fact]
    public async Task StreamingSectionOutlet_NonStreamingErrorBoundary_RecoversBeforeInitialFlush()
    {
        var releaseStreaming = new TaskCompletionSource();
        var httpContext = GetTestHttpContext();
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "mixed-section"));
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        var completion = new RazorComponentResult(typeof(NonStreamingSectionErrorHost), new
        {
            ReleaseStreaming = releaseStreaming.Task,
            StreamingOutlet = true,
        }).ExecuteAsync(httpContext);
        try
        {
            await WaitForContentWrittenAsync(responseBody);
            Assert.False(completion.IsCompleted);
            Assert.Contains("Boundary fallback", GetStringContent(responseBody));
            Assert.DoesNotContain("Mismatched content", GetStringContent(responseBody));
        }
        finally
        {
            releaseStreaming.SetResult();
            await completion;
        }
    }

    [Fact]
    public async Task FirstFlushSectionValidation_DoesNotFaultSameScopeErrorResult()
    {
        var releaseStreaming = new TaskCompletionSource();
        var httpContext = GetTestHttpContext();
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "first-flush-error"));
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new RazorComponentResult(typeof(FirstFlushSectionErrorHost), new
                {
                    ReleaseStreaming = releaseStreaming.Task,
                }).ExecuteAsync(httpContext));
            Assert.Contains("section-one", exception.Message);
            Assert.Equal(0, responseBody.Length);

            httpContext.Response.Clear();
            httpContext.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature { Error = exception });
            await new RazorComponentResult(typeof(PlainErrorEndpoint)).ExecuteAsync(httpContext).WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(releaseStreaming.Task.IsCompleted);
            Assert.Equal("Error endpoint rendered", GetStringContent(responseBody));
        }
        finally
        {
            releaseStreaming.TrySetResult();
        }
    }

    [Fact]
    public async Task SectionValidation_HandledRedirectDoesNotFaultNextRoot()
    {
        var httpContext = GetTestHttpContext();
        var renderer = httpContext.RequestServices.GetRequiredService<EndpointHtmlRenderer>();
        await renderer.PrerenderComponentAsync(httpContext, typeof(SectionRedirectHost), null, ParameterView.Empty);
        Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        Assert.Equal("https://test/redirected", httpContext.Response.Headers.Location);

        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(PlainErrorEndpoint), null, ParameterView.Empty);
        using var writer = new StringWriter();
        await renderer.Dispatcher.InvokeAsync(() => result.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default));
        Assert.Equal("Error endpoint rendered", writer.ToString());
    }

    [Fact]
    public async Task AbandonedSectionValidation_DoesNotFaultSameScopeErrorEndpoint()
    {
        var httpContext = GetTestHttpContext();
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "error-endpoint"));
        var renderer = httpContext.RequestServices.GetRequiredService<EndpointHtmlRenderer>();
        renderer.SetHttpContext(httpContext);
        var failingComponent = new MultiplePendingSectionValidationHost();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.Dispatcher.InvokeAsync(
            () => renderer.BeginRenderingComponent(failingComponent, ParameterView.Empty)));
        Assert.Contains("section-one", exception.Message);

        httpContext.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature { Error = exception });
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        await new RazorComponentResult(typeof(PlainErrorEndpoint)).ExecuteAsync(httpContext);

        Assert.Equal("Error endpoint rendered", GetStringContent(responseBody));
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
            .AddSingleton<IHostEnvironment>(mockWebHostEnvironment)
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
            .AddKeyedSingleton<Func<IComponentRenderMode, IComponentRenderMode, bool>>(
                typeof(SectionOutlet), EndpointHtmlRenderer.AreSectionRenderModesCompatible)
            .AddSingleton<IErrorBoundaryLogger, TestErrorBoundaryLogger>()
            .AddLogging();
        serviceCollection.AddOptions<RazorComponentsServiceOptions>();

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

    [StreamRendering(true)]
    private sealed class StreamingSectionErrorBoundaryHost : ComponentBase
    {
        [Parameter] public Task ReleaseProvider { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<StreamingSectionRegion>(0);
            builder.AddComponentParameter(1, nameof(StreamingSectionRegion.ReleaseProvider), ReleaseProvider);
            builder.CloseComponent();
        }
    }

    private sealed class StreamingSectionRegion : ComponentBase
    {
        private bool _showProvider;

        [Parameter] public Task ReleaseProvider { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await ReleaseProvider;
            _showProvider = true;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<ErrorBoundary>(0);
            builder.AddComponentParameter(1, nameof(ErrorBoundary.ErrorContent), (RenderFragment<Exception>)(_ => content => content.AddContent(0, "Boundary fallback")));
            builder.AddComponentParameter(2, nameof(ErrorBoundary.ChildContent), (RenderFragment)(child =>
            {
                child.OpenComponent<SectionOutlet>(0);
                child.AddComponentParameter(1, nameof(SectionOutlet.SectionName), "streaming-section");
                child.AddComponentRenderMode(RenderMode.InteractiveWebAssembly);
                child.CloseComponent();
            }));
            builder.CloseComponent();
            if (_showProvider)
            {
                builder.OpenComponent<SectionContent>(3);
                builder.AddComponentParameter(4, nameof(SectionContent.SectionName), "streaming-section");
                builder.AddComponentParameter(5, nameof(SectionContent.ChildContent), (RenderFragment)(content => content.AddContent(0, "Mismatched content")));
                builder.AddComponentRenderMode(RenderMode.InteractiveServer);
                builder.CloseComponent();
            }
        }
    }

    private class NonStreamingSectionErrorHost : ComponentBase
    {
        [Parameter] public Task ReleaseFallback { get; set; }
        [Parameter] public TaskCompletionSource FallbackStarted { get; set; }
        [Parameter] public Task ReleaseStreaming { get; set; }
        [Parameter] public bool StreamingOutlet { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<ErrorBoundary>(0);
            builder.AddComponentParameter(1, nameof(ErrorBoundary.ErrorContent), (RenderFragment<Exception>)(_ => content =>
            {
                if (ReleaseFallback is null)
                {
                    content.AddContent(0, "Boundary fallback");
                }
                else
                {
                    content.OpenComponent<AsyncSectionFallback>(1);
                    content.AddComponentParameter(2, nameof(AsyncSectionFallback.Release), ReleaseFallback);
                    content.AddComponentParameter(3, nameof(AsyncSectionFallback.Started), FallbackStarted);
                    content.CloseComponent();
                }
            }));
            builder.AddComponentParameter(2, nameof(ErrorBoundary.ChildContent), (RenderFragment)(child =>
            {
                if (StreamingOutlet)
                {
                    child.OpenComponent<StreamingInitialSectionOutlet>(0);
                    child.CloseComponent();
                }
                else
                {
                    child.OpenComponent<SectionOutlet>(1);
                    child.AddComponentParameter(2, nameof(SectionOutlet.SectionName), "initial-section");
                    child.AddComponentRenderMode(RenderMode.InteractiveServer);
                    child.CloseComponent();
                }
            }));
            builder.CloseComponent();
            builder.OpenComponent<SectionContent>(3);
            builder.AddComponentParameter(4, nameof(SectionContent.SectionName), "initial-section");
            builder.AddComponentParameter(5, nameof(SectionContent.ChildContent), (RenderFragment)(content => content.AddContent(0, "Mismatched content")));
            builder.CloseComponent();
            if (ReleaseStreaming is not null)
            {
                builder.OpenComponent<UnrelatedStreamingSectionSibling>(6);
                builder.AddComponentParameter(7, nameof(UnrelatedStreamingSectionSibling.Release), ReleaseStreaming);
                builder.CloseComponent();
            }
        }
    }

    [StreamRendering]
    private sealed class StreamingInitialSectionErrorHost : NonStreamingSectionErrorHost;

    [StreamRendering]
    private sealed class StreamingInitialSectionOutlet : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<SectionOutlet>(0);
            builder.AddComponentParameter(1, nameof(SectionOutlet.SectionName), "initial-section");
            builder.AddComponentRenderMode(RenderMode.InteractiveServer);
            builder.CloseComponent();
        }
    }

    private sealed class FirstFlushSectionErrorHost : ComponentBase
    {
        [Parameter] public Task ReleaseStreaming { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<UnrelatedStreamingSectionSibling>(0);
            builder.AddComponentParameter(1, nameof(UnrelatedStreamingSectionSibling.Release), ReleaseStreaming);
            builder.CloseComponent();
            builder.OpenComponent<MultiplePendingSectionValidationHost>(2);
            builder.CloseComponent();
        }
    }

    private sealed class AsyncSectionFallback : ComponentBase
    {
        private bool _loaded;
        [Parameter] public Task Release { get; set; }
        [Parameter] public TaskCompletionSource Started { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Started.SetResult();
            await Release;
            _loaded = true;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => builder.AddContent(0, _loaded ? "Fallback loaded" : "Fallback loading");
    }

    [StreamRendering]
    private sealed class UnrelatedStreamingSectionSibling : ComponentBase
    {
        [Parameter] public Task Release { get; set; }
        protected override Task OnInitializedAsync() => Release;
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Streaming sibling");
    }

    private sealed class SectionRedirectHost : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MultiplePendingSectionValidationHost>(0);
            builder.CloseComponent();
            builder.OpenComponent<SectionRedirectWrapper>(1);
            builder.CloseComponent();
        }
    }

    private sealed class SectionRedirectWrapper : ComponentBase
    {
        [Parameter] public int Depth { get; set; } = 5;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // Render the redirect after the section render-mode boundaries and outlets initialize.
            if (Depth > 0)
            {
                builder.OpenComponent<SectionRedirectWrapper>(0);
                builder.AddComponentParameter(1, nameof(Depth), Depth - 1);
                builder.CloseComponent();
            }
            else
            {
                builder.OpenComponent<RedirectAfterSections>(2);
                builder.CloseComponent();
            }
        }
    }

    private sealed class RedirectAfterSections : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        protected override void OnInitialized() => Navigation.NavigateTo("/redirected");
    }

    private sealed class MultiplePendingSectionValidationHost : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            AddMismatchedSection(builder, 0, "section-one");
            AddMismatchedSection(builder, 10, "section-two");
        }

        private static void AddMismatchedSection(RenderTreeBuilder builder, int sequence, string name)
        {
            builder.OpenComponent<SectionOutlet>(sequence);
            builder.AddComponentParameter(sequence + 1, nameof(SectionOutlet.SectionName), name);
            builder.AddComponentRenderMode(RenderMode.InteractiveServer);
            builder.CloseComponent();
            builder.OpenComponent<SectionContent>(sequence + 2);
            builder.AddComponentParameter(sequence + 3, nameof(SectionContent.SectionName), name);
            builder.AddComponentParameter(sequence + 4, nameof(SectionContent.ChildContent), (RenderFragment)(content => content.AddContent(0, name)));
            builder.AddComponentRenderMode(RenderMode.InteractiveWebAssembly);
            builder.CloseComponent();
        }
    }

    private sealed class PlainErrorEndpoint : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => builder.AddContent(0, "Error endpoint rendered");
    }

    private sealed class TestErrorBoundaryLogger : IErrorBoundaryLogger
    {
        public ValueTask LogErrorAsync(Exception exception)
            => ValueTask.CompletedTask;
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
