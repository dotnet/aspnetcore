using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class HtmlHelperComponentExtensionsTests
    {
        private static readonly Regex ContentWrapperRegex = new Regex(
            "<!-- M.A.C.Component: {\"circuitId\":\"[^\"]+\",\"rendererId\":0,\"componentId\":0} -->(?<content>.*)<!-- M.A.C.Component: 0 -->",
            RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(1)); // Treat the entire input string as a single line

        [Fact]
        public async Task PrerenderComponentAsync_ThrowsInvalidOperationException_IfNoPrerendererHasBeenRegistered()
        {
            // Arrange
            var helper = CreateHelper(null, s => { });
            var writer = new StringWriter();
            var expectedmessage = $"No 'IComponentPrerenderer' implementation has been registered in the dependency injection container. " +
                    $"This typically means a call to 'services.AddServerSideBlazor()' is missing in 'Startup.ConfigureServices'.";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => helper.RenderComponentAsync<TestComponent>());

            // Assert
            Assert.Equal(expectedmessage, exception.Message);
        }

        [Fact]
        public async Task CanRender_ParameterlessComponent()
        {
            // Arrange
            var helper = CreateHelper();

            // Act
            var result = await helper.RenderComponentAsync<TestComponent>();
            var unwrappedContent = GetUnwrappedContent(result);

            // Assert
            Assert.Equal("<h1>Hello world!</h1>", unwrappedContent);
        }

        [Fact]
        public async Task CanRender_ComponentWithParametersObject()
        {
            // Arrange
            var helper = CreateHelper();

            // Act
            var result = await helper.RenderComponentAsync<GreetingComponent>(new
            {
                Name = "Guest"
            });

            var unwrappedContent = GetUnwrappedContent(result);

            // Assert
            Assert.Equal("<p>Hello Guest!</p>", unwrappedContent);
        }

        [Fact]
        public async Task CanCatch_ComponentWithSynchronousException()
        {
            // Arrange
            var helper = CreateHelper();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => helper.RenderComponentAsync<ExceptionComponent>(new
            {
                IsAsync = false
            }));

            // Assert
            Assert.Equal("Threw an exception synchronously", exception.Message);
        }

        [Fact]
        public async Task CanCatch_ComponentWithAsynchronousException()
        {
            // Arrange
            var helper = CreateHelper();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => helper.RenderComponentAsync<ExceptionComponent>(new
            {
                IsAsync = true
            }));

            // Assert
            Assert.Equal("Threw an exception asynchronously", exception.Message);
        }

        [Fact]
        public async Task Rendering_ComponentWithJsInteropThrows()
        {
            // Arrange
            var helper = CreateHelper();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => helper.RenderComponentAsync<ExceptionComponent>(new
            {
                JsInterop = true
            }));

            // Assert
            Assert.Equal("JavaScript interop calls cannot be issued at this time. This is because the component is being " +
                    "prerendered and the page has not yet loaded in the browser or because the circuit is currently disconnected. " +
                    "Components must wrap any JavaScript interop calls in conditional logic to ensure those interop calls are not " +
                    "attempted during prerendering or while the client is disconnected.",
                exception.Message);
        }

        [Fact]
        public async Task UriHelperRedirect_ThrowsInvalidOperationException_WhenResponseHasAlreadyStarted()
        {
            // Arrange
            var ctx = new DefaultHttpContext();
            ctx.Request.Scheme = "http";
            ctx.Request.Host = new HostString("localhost");
            ctx.Request.PathBase = "/base";
            ctx.Request.Path = "/path";
            ctx.Request.QueryString = new QueryString("?query=value");
            var responseMock = new Mock<IHttpResponseFeature>();
            responseMock.Setup(r => r.HasStarted).Returns(true);
            ctx.Features.Set(responseMock.Object);
            var helper = CreateHelper(ctx);
            var writer = new StringWriter();

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => helper.RenderComponentAsync<RedirectComponent>(new
            {
                RedirectUri = "http://localhost/redirect"
            }));

            Assert.Equal("A navigation command was attempted during prerendering after the server already started sending the response. " +
                            "Navigation commands can not be issued during server-side prerendering after the response from the server has started. Applications must buffer the" +
                            "reponse and avoid using features like FlushAsync() before all components on the page have been rendered to prevent failed navigation commands.",
                exception.Message);
        }

        [Fact]
        public async Task HtmlHelper_Redirects_WhenComponentNavigates()
        {
            // Arrange
            var ctx = new DefaultHttpContext();
            ctx.Request.Scheme = "http";
            ctx.Request.Host = new HostString("localhost");
            ctx.Request.PathBase = "/base";
            ctx.Request.Path = "/path";
            ctx.Request.QueryString = new QueryString("?query=value");
            var helper = CreateHelper(ctx);

            // Act
            await helper.RenderComponentAsync<RedirectComponent>(new
            {
                RedirectUri = "http://localhost/redirect"
            });

            // Assert
            Assert.Equal(302, ctx.Response.StatusCode);
            Assert.Equal("http://localhost/redirect", ctx.Response.Headers[HeaderNames.Location]);
        }

        [Fact]
        public async Task HtmlHelper_AvoidsRendering_WhenNavigationHasHappened()
        {
            // Arrange
            var ctx = new DefaultHttpContext();
            ctx.Request.Scheme = "http";
            ctx.Request.Host = new HostString("localhost");
            ctx.Request.PathBase = "/base";
            ctx.Request.Path = "/path";
            ctx.Request.QueryString = new QueryString("?query=value");
            var helper = CreateHelper(ctx);
            var stringWriter = new StringWriter();

            await helper.RenderComponentAsync<RedirectComponent>(new
            {
                RedirectUri = "http://localhost/redirect"
            });

            // Act
            var result = await helper.RenderComponentAsync<GreetingComponent>(new { Name = "George" });

            // Assert
            Assert.NotNull(result);
            result.WriteTo(stringWriter, HtmlEncoder.Default);
            Assert.Equal("", stringWriter.ToString());
        }

        [Fact]
        public async Task CanRender_AsyncComponent()
        {
            // Arrange
            var helper = CreateHelper();
            var expectedContent = @"<table>
<thead>
<tr>
<th>Date</th>
<th>Summary</th>
<th>F</th>
<th>C</th>
</tr>
</thead>
<tbody>
<tr>
<td>06/05/2018</td>
<td>Freezing</td>
<td>33</td>
<td>33</td>
</tr>
<tr>
<td>07/05/2018</td>
<td>Bracing</td>
<td>57</td>
<td>57</td>
</tr>
<tr>
<td>08/05/2018</td>
<td>Freezing</td>
<td>9</td>
<td>9</td>
</tr>
<tr>
<td>09/05/2018</td>
<td>Balmy</td>
<td>4</td>
<td>4</td>
</tr>
<tr>
<td>10/05/2018</td>
<td>Chilly</td>
<td>29</td>
<td>29</td>
</tr>
</tbody>
</table>";

            // Act
            var result = await helper.RenderComponentAsync<AsyncComponent>();
            var unwrappedContent = GetUnwrappedContent(result);

            // Assert
            Assert.Equal(expectedContent.Replace("\r\n", "\n"), unwrappedContent);
        }

        private string GetUnwrappedContent(IHtmlContent rawResult)
        {
            var writer = new StringWriter();
            rawResult.WriteTo(writer, HtmlEncoder.Default);

            return ContentWrapperRegex.Match(writer.ToString())
                .Groups["content"].Value
                .Replace("\r\n", "\n");
        }

        private class TestComponent : IComponent
        {
            private RenderHandle _renderHandle;

            public void Configure(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public Task SetParametersAsync(ParameterCollection parameters)
            {
                _renderHandle.Render(builder =>
                {
                    builder.OpenElement(1, "h1");
                    builder.AddContent(2, "Hello world!");
                    builder.CloseElement();
                });
                return Task.CompletedTask;
            }
        }

        private class RedirectComponent : ComponentBase
        {
            [Inject] IUriHelper UriHelper { get; set; }

            [Parameter] public string RedirectUri { get; set; }

            [Parameter] public bool Force { get; set; }

            protected override void OnInitialized()
            {
                UriHelper.NavigateTo(RedirectUri, Force);
            }
        }

        private class ExceptionComponent : ComponentBase
        {
            [Parameter] bool IsAsync { get; set; }

            [Parameter] bool JsInterop { get; set; }

            [Inject] IJSRuntime JsRuntime { get; set; }

            protected override async Task OnParametersSetAsync()
            {
                if (JsInterop)
                {
                    await JsRuntime.InvokeAsync<int>("window.alert", "Interop!");
                }

                if (!IsAsync)
                {
                    throw new InvalidOperationException("Threw an exception synchronously");
                }
                else
                {
                    await Task.Yield();
                    throw new InvalidOperationException("Threw an exception asynchronously");
                }
            }
        }

        private class GreetingComponent : ComponentBase
        {
            [Parameter] public string Name { get; set; }

            protected override void OnParametersSet()
            {
                base.OnParametersSet();
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                base.BuildRenderTree(builder);
                builder.OpenElement(1, "p");
                builder.AddContent(2, $"Hello {Name}!");
                builder.CloseElement();
            }
        }

        private class AsyncComponent : ComponentBase
        {
            private static WeatherRow[] _weatherData = new[]
            {
                new WeatherRow
                {
                    DateFormatted = "06/05/2018",
                    TemperatureC = 1,
                    Summary = "Freezing",
                    TemperatureF = 33
                },
                new WeatherRow
                {
                    DateFormatted = "07/05/2018",
                    TemperatureC = 14,
                    Summary = "Bracing",
                    TemperatureF = 57
                },
                new WeatherRow
                {
                    DateFormatted = "08/05/2018",
                    TemperatureC = -13,
                    Summary = "Freezing",
                    TemperatureF = 9
                },
                new WeatherRow
                {
                    DateFormatted = "09/05/2018",
                    TemperatureC = -16,
                    Summary = "Balmy",
                    TemperatureF = 4
                },
                new WeatherRow
                {
                    DateFormatted = "10/05/2018",
                    TemperatureC = 2,
                    Summary = "Chilly",
                    TemperatureF = 29
                }
            };

            public class WeatherRow
            {
                public string DateFormatted { get; set; }
                public int TemperatureC { get; set; }
                public string Summary { get; set; }
                public int TemperatureF { get; set; }
            }

            public WeatherRow[] RowsToDisplay { get; set; }

            protected override async Task OnParametersSetAsync()
            {
                // Simulate an async workflow.
                await Task.Yield();
                RowsToDisplay = _weatherData;
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                base.BuildRenderTree(builder);

                builder.OpenElement(0, "table");
                builder.AddMarkupContent(1, "\n");
                builder.OpenElement(2, "thead");
                builder.AddMarkupContent(3, "\n");
                builder.OpenElement(4, "tr");
                builder.AddMarkupContent(5, "\n");

                builder.OpenElement(6, "th");
                builder.AddContent(7, "Date");
                builder.CloseElement();
                builder.AddMarkupContent(8, "\n");

                builder.OpenElement(9, "th");
                builder.AddContent(10, "Summary");
                builder.CloseElement();
                builder.AddMarkupContent(11, "\n");

                builder.OpenElement(12, "th");
                builder.AddContent(13, "F");
                builder.CloseElement();
                builder.AddMarkupContent(14, "\n");

                builder.OpenElement(15, "th");
                builder.AddContent(16, "C");
                builder.CloseElement();
                builder.AddMarkupContent(17, "\n");

                builder.CloseElement();
                builder.AddMarkupContent(18, "\n");
                builder.CloseElement();
                builder.AddMarkupContent(19, "\n");
                builder.OpenElement(20, "tbody");
                builder.AddMarkupContent(21, "\n");
                if (RowsToDisplay != null)
                {
                    foreach (var element in RowsToDisplay)
                    {
                        builder.OpenElement(22, "tr");
                        builder.AddMarkupContent(23, "\n");

                        builder.OpenElement(24, "td");
                        builder.AddContent(25, element.DateFormatted);
                        builder.CloseElement();
                        builder.AddMarkupContent(26, "\n");

                        builder.OpenElement(27, "td");
                        builder.AddContent(28, element.Summary);
                        builder.CloseElement();
                        builder.AddMarkupContent(29, "\n");

                        builder.OpenElement(30, "td");
                        builder.AddContent(31, element.TemperatureF);
                        builder.CloseElement();
                        builder.AddMarkupContent(32, "\n");

                        builder.OpenElement(33, "td");
                        builder.AddContent(34, element.TemperatureF);
                        builder.CloseElement();
                        builder.AddMarkupContent(35, "\n");

                        builder.CloseElement();
                        builder.AddMarkupContent(36, "\n");
                    }
                }

                builder.CloseElement();
                builder.AddMarkupContent(37, "\n");

                builder.CloseElement();
            }
        }

        private static IHtmlHelper CreateHelper(HttpContext ctx = null, Action<IServiceCollection> configureServices = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddLogging();
            services.AddDataProtection();
            services.AddSingleton(HtmlEncoder.Default);
            configureServices = configureServices ?? (s => s.AddServerSideBlazor());
            configureServices?.Invoke(services);

            var helper = new Mock<IHtmlHelper>();
            var context = ctx ?? new DefaultHttpContext();
            context.RequestServices = services.BuildServiceProvider();
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost");
            context.Request.PathBase = "/base";
            context.Request.Path = "/path";
            context.Request.QueryString = QueryString.FromUriComponent("?query=value");

            helper.Setup(h => h.ViewContext)
                .Returns(new ViewContext()
                {
                    HttpContext = context
                });
            return helper.Object;
        }
    }
}