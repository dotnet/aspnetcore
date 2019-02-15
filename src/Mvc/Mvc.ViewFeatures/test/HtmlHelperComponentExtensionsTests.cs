// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Test
{
    public class HtmlHelperComponentExtensionsTests
    {
        [Fact]
        public async Task CanRender_ParameterlessComponent()
        {
            // Arrange
            var helper = CreateHelper();
            var writer = new StringWriter();

            // Act
            var result = await helper.RenderComponentAsync<TestComponent>();
            result.WriteTo(writer, HtmlEncoder.Default);
            var content = writer.ToString();

            // Assert
            Assert.Equal("<h1>Hello world!</h1>", content);
        }

        [Fact]
        public async Task CanRender_ComponentWithParametersObject()
        {
            // Arrange
            var helper = CreateHelper();
            var writer = new StringWriter();

            // Act
            var result = await helper.RenderComponentAsync<GreetingComponent>(new
            {
                Name = "Steve"
            });
            result.WriteTo(writer, HtmlEncoder.Default);
            var content = writer.ToString();

            // Assert
            Assert.Equal("<p>Hello Steve!</p>", content);
        }

        [Fact]
        public async Task CanRender_AsyncComponent()
        {
            // Arrange
            var helper = CreateHelper();
            var writer = new StringWriter();
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
            result.WriteTo(writer, HtmlEncoder.Default);
            var content = writer.ToString();

            // Assert
            Assert.Equal(expectedContent.Replace("\r\n","\n"), content);
        }

        private static IHtmlHelper CreateHelper(Action<IServiceCollection> configureServices = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<HtmlEncoder>(HtmlEncoder.Default);
            configureServices?.Invoke(serviceCollection);

            var helper = new Mock<IHtmlHelper>();
            helper.Setup(h => h.ViewContext)
                .Returns(new ViewContext()
                {
                    HttpContext = new DefaultHttpContext()
                    {
                        RequestServices = serviceCollection.BuildServiceProvider()
                    }
                });
            return helper.Object;
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
                    var s = 0;
                    builder.OpenElement(s++, "h1");
                    builder.AddContent(s++, "Hello world!");
                    builder.CloseElement();
                });
                return Task.CompletedTask;
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
                var s = 0;
                base.BuildRenderTree(builder);
                builder.OpenElement(s++, "p");
                builder.AddContent(s++, $"Hello {Name}!");
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
                var s = 0;
                builder.OpenElement(s++, "table");
                builder.AddMarkupContent(s++, "\n");
                builder.OpenElement(s++, "thead");
                builder.AddMarkupContent(s++, "\n");
                builder.OpenElement(s++, "tr");
                builder.AddMarkupContent(s++, "\n");

                builder.OpenElement(s++, "th");
                builder.AddContent(s++, "Date");
                builder.CloseElement();
                builder.AddMarkupContent(s++, "\n");

                builder.OpenElement(s++, "th");
                builder.AddContent(s++, "Summary");
                builder.CloseElement();
                builder.AddMarkupContent(s++, "\n");

                builder.OpenElement(s++, "th");
                builder.AddContent(s++, "F");
                builder.CloseElement();
                builder.AddMarkupContent(s++, "\n");

                builder.OpenElement(s++, "th");
                builder.AddContent(s++, "C");
                builder.CloseElement();
                builder.AddMarkupContent(s++, "\n");

                builder.CloseElement();
                builder.AddMarkupContent(s++, "\n");
                builder.CloseElement();
                builder.AddMarkupContent(s++, "\n");
                builder.OpenElement(s++, "tbody");
                builder.AddMarkupContent(s++, "\n");
                if (RowsToDisplay != null)
                {
                    var s2 = s;
                    foreach (var element in RowsToDisplay)
                    {
                        s = s2;
                        builder.OpenElement(s++, "tr");
                        builder.AddMarkupContent(s++, "\n");

                        builder.OpenElement(s++, "td");
                        builder.AddContent(s++, element.DateFormatted);
                        builder.CloseElement();
                        builder.AddMarkupContent(s++, "\n");

                        builder.OpenElement(s++, "td");
                        builder.AddContent(s++, element.Summary);
                        builder.CloseElement();
                        builder.AddMarkupContent(s++, "\n");

                        builder.OpenElement(s++, "td");
                        builder.AddContent(s++, element.TemperatureF);
                        builder.CloseElement();
                        builder.AddMarkupContent(s++, "\n");

                        builder.OpenElement(s++, "td");
                        builder.AddContent(s++, element.TemperatureF);
                        builder.CloseElement();
                        builder.AddMarkupContent(s++, "\n");

                        builder.CloseElement();
                        builder.AddMarkupContent(s++, "\n");
                    }
                }

                builder.CloseElement();
                builder.AddMarkupContent(s++, "\n");

                builder.CloseElement();
            }
        }
    }
}
