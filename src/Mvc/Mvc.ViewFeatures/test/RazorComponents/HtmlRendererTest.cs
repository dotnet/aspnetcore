// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Rendering;

public class HtmlRendererTest
{
    protected readonly HtmlEncoder _encoder = HtmlEncoder.Default;

    [Fact]
    public void RenderComponentAsync_CanRenderEmptyElement()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", ">", "</", "p", ">" };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.CloseElement();
        })).BuildServiceProvider();
        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_CanRenderSimpleComponent()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", ">", "Hello world!", "</", "p", ">" };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddContent(1, "Hello world!");
            rtb.CloseElement();
        })).BuildServiceProvider();
        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_HtmlEncodesContent()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", ">", "&lt;Hello world!&gt;", "</", "p", ">" };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddContent(1, "<Hello world!>");
            rtb.CloseElement();
        })).BuildServiceProvider();
        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }


    [Fact]
    public void RenderComponentAsync_DoesNotEncodeMarkup()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", ">", "<span>Hello world!</span>", "</", "p", ">" };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddMarkupContent(1, "<span>Hello world!</span>");
            rtb.CloseElement();
        })).BuildServiceProvider();
        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_CanRenderWithAttributes()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", " ", "class", "=", "\"", "lead", "\"", ">", "Hello world!", "</", "p", ">" };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddAttribute(1, "class", "lead");
            rtb.AddContent(2, "Hello world!");
            rtb.CloseElement();
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_SkipsDuplicatedAttribute()
    {
        // Arrange
        var expectedHtml = new[]
        {
                "<", "p", " ",
                    "another", "=", "\"", "another-value", "\"", " ",
                    "Class", "=", "\"", "test2", "\"", ">",
                    "Hello world!",
                "</", "p", ">"
            };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddAttribute(1, "class", "test1");
            rtb.AddAttribute(2, "another", "another-value");
            rtb.AddMultipleAttributes(3, new Dictionary<string, object>()
            {
                    { "Class", "test2" }, // Matching is case-insensitive.
            });
            rtb.AddContent(4, "Hello world!");
            rtb.CloseElement();
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_HtmlEncodesAttributeValues()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", " ", "class", "=", "\"", "&lt;lead", "\"", ">", "Hello world!", "</", "p", ">" };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddAttribute(1, "class", "<lead");
            rtb.AddContent(2, "Hello world!");
            rtb.CloseElement();
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_CanRenderBooleanAttributes()
    {
        // Arrange
        var expectedHtml = new[] { "<", "input", " ", "disabled", " />" };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "input");
            rtb.AddAttribute(1, "disabled", true);
            rtb.CloseElement();
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_DoesNotRenderBooleanAttributesWhenValueIsFalse()
    {
        // Arrange
        var expectedHtml = new[] { "<", "input", " />" };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "input");
            rtb.AddAttribute(1, "disabled", false);
            rtb.CloseElement();
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_CanRenderWithChildren()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", ">", "<", "span", ">", "Hello world!", "</", "span", ">", "</", "p", ">" };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.OpenElement(1, "span");
            rtb.AddContent(2, "Hello world!");
            rtb.CloseElement();
            rtb.CloseElement();
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_CanRenderWithMultipleChildren()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", ">",
                "<", "span", ">", "Hello world!", "</", "span", ">",
                "<", "span", ">", "Bye Bye world!", "</", "span", ">",
                "</", "p", ">"
            };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.OpenElement(1, "span");
            rtb.AddContent(2, "Hello world!");
            rtb.CloseElement();
            rtb.OpenElement(3, "span");
            rtb.AddContent(4, "Bye Bye world!");
            rtb.CloseElement();
            rtb.CloseElement();
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_MarksSelectedOptionsAsSelected()
    {
        // Arrange
        var expectedHtml = "<p>" +
            @"<select unrelated-attribute-before=""a"" value=""b"" unrelated-attribute-after=""c"">" +
            @"<option unrelated-attribute=""a"" value=""a"">Pick value a</option>" +
            @"<option unrelated-attribute=""a"" value=""b"" selected>Pick value b</option>" +
            @"<option unrelated-attribute=""a"" value=""c"">Pick value c</option>" +
            "</select>" +
            @"<option value=""b"">unrelated option</option>" +
            "</p>";
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.OpenElement(1, "select");
            rtb.AddAttribute(2, "unrelated-attribute-before", "a");
            rtb.AddAttribute(3, "value", "b");
            rtb.AddAttribute(4, "unrelated-attribute-after", "c");

            foreach (var optionValue in new[] { "a", "b", "c" })
            {
                rtb.OpenElement(5, "option");
                rtb.AddAttribute(6, "unrelated-attribute", "a");
                rtb.AddAttribute(7, "value", optionValue);
                rtb.AddContent(8, $"Pick value {optionValue}");
                rtb.CloseElement(); // option
            }

            rtb.CloseElement(); // select

            rtb.OpenElement(9, "option"); // To show other value-matching options don't get marked as selected
            rtb.AddAttribute(10, "value", "b");
            rtb.AddContent(11, "unrelated option");
            rtb.CloseElement(); // option

            rtb.CloseElement(); // p
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_MarksSelectedOptionsAsSelected_WithOptGroups()
    {
        // Arrange
        var expectedHtml =
            @"<select value=""beta"">" +
            @"<optgroup><option value=""alpha"">alpha</option></optgroup>" +
            @"<optgroup><option value=""beta"" selected>beta</option></optgroup>" +
            @"<optgroup><option value=""gamma"">gamma</option></optgroup>" +
            "</select>";
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "select");
            rtb.AddAttribute(1, "value", "beta");

            foreach (var optionValue in new[] { "alpha", "beta", "gamma" })
            {
                rtb.OpenElement(2, "optgroup");
                rtb.OpenElement(3, "option");
                rtb.AddAttribute(4, "value", optionValue);
                rtb.AddContent(5, optionValue);
                rtb.CloseElement(); // option
                rtb.CloseElement(); // optgroup
            }

            rtb.CloseElement(); // select
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_CanRenderComponentAsyncWithChildrenComponents()
    {
        // Arrange
        var expectedHtml = new[] {
                "<", "p", ">", "<", "span", ">", "Hello world!", "</", "span", ">", "</", "p", ">",
                "<", "span", ">", "Child content!", "</", "span", ">"
            };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.OpenElement(1, "span");
            rtb.AddContent(2, "Hello world!");
            rtb.CloseElement();
            rtb.CloseElement();
            rtb.OpenComponent(3, typeof(ChildComponent));
            rtb.AddAttribute(4, "Value", "Child content!");
            rtb.CloseComponent();
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_ComponentReferenceNoops()
    {
        // Arrange
        var expectedHtml = new[] {
                "<", "p", ">", "<", "span", ">", "Hello world!", "</", "span", ">", "</", "p", ">",
                "<", "span", ">", "Child content!", "</", "span", ">"
            };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.OpenElement(1, "span");
            rtb.AddContent(2, "Hello world!");
            rtb.CloseElement();
            rtb.CloseElement();
            rtb.OpenComponent(3, typeof(ChildComponent));
            rtb.AddAttribute(4, "Value", "Child content!");
            rtb.AddComponentReferenceCapture(5, cr => { });
            rtb.CloseComponent();
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_CanPassParameters()
    {
        // Arrange
        var expectedHtml = new[] {
                "<", "p", ">", "<", "input", " ", "value", "=", "\"", "5", "\"", " />", "</", "p", ">" };

        RenderFragment Content(ParameterView pc) => new RenderFragment((RenderTreeBuilder rtb) =>
        {
            rtb.OpenElement(0, "p");
            rtb.OpenElement(1, "input");
            rtb.AddAttribute(2, "change", pc.GetValueOrDefault<Action<ChangeEventArgs>>("update"));
            rtb.AddAttribute(3, "value", pc.GetValueOrDefault<int>("value"));
            rtb.CloseElement();
            rtb.CloseElement();
        });

        var serviceProvider = new ServiceCollection()
            .AddSingleton(new Func<ParameterView, RenderFragment>(Content))
            .BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        Action<ChangeEventArgs> change = (ChangeEventArgs changeArgs) => throw new InvalidOperationException();

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<ComponentWithParameters>(
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                    { "update", change },
                    { "value", 5 }
            }))));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_CanRenderComponentAsyncWithRenderFragmentContent()
    {
        // Arrange
        var expectedHtml = new[] {
                "<", "p", ">", "<", "span", ">", "Hello world!", "</", "span", ">", "</", "p", ">" };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.OpenElement(1, "span");
            rtb.AddContent(2,
                // This internally creates a region frame.
                rf => rf.AddContent(0, "Hello world!"));
            rtb.CloseElement();
            rtb.CloseElement();
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    [Fact]
    public void RenderComponentAsync_ElementRefsNoops()
    {
        // Arrange
        var expectedHtml = new[]
        {
            "<", "p", ">", "<", "span", ">", "Hello world!", "</", "span", ">", "</", "p", ">"
        };
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddElementReferenceCapture(1, er => { });
            rtb.OpenElement(2, "span");
            rtb.AddContent(3,
                // This internally creates a region frame.
                rf => rf.AddContent(0, "Hello world!"));
            rtb.CloseElement();
            rtb.CloseElement();
        })).BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = GetResult(htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterView.Empty)));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result);
    }

    private IHtmlContent GetResult(Task<ComponentRenderedText> task)
    {
        Assert.True(task.IsCompleted);
        if (task.IsCompletedSuccessfully)
        {
            return task.Result.HtmlContent;
        }
        else
        {
            ExceptionDispatchInfo.Capture(task.Exception).Throw();
            throw new InvalidOperationException("We will never hit this line");
        }
    }

    private void AssertHtmlContentEquals(IEnumerable<string> expected, IHtmlContent actual)
    {
        var expectedString = string.Concat(expected);
        AssertHtmlContentEquals(expectedString, actual);
    }

    private void AssertHtmlContentEquals(string expected, IHtmlContent actual)
    {
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(actual, _encoder));
    }

    private class ComponentWithParameters : IComponent
    {
        public RenderHandle RenderHandle { get; private set; }

        public void Attach(RenderHandle renderHandle)
        {
            RenderHandle = renderHandle;
        }

        [Inject]
        Func<ParameterView, RenderFragment> CreateRenderFragment { get; set; }

        public Task SetParametersAsync(ParameterView parameters)
        {
            RenderHandle.Render(CreateRenderFragment(parameters));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CanRender_AsyncComponent()
    {
        // Arrange
        var expectedHtml = new[] {
                "<", "p", ">", "20", "</", "p", ">" };
        var serviceProvider = new ServiceCollection().AddSingleton<AsyncComponent>().BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = await htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<AsyncComponent>(ParameterView.FromDictionary(new Dictionary<string, object>
        {
            ["Value"] = 10
        })));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result.HtmlContent);
    }

    [Fact]
    public async Task CanRender_NestedAsyncComponents()
    {
        // Arrange
        var expectedHtml = new[]
        {
                "<", "p", ">", "20", "</", "p", ">",
                "<", "p", ">", "80", "</", "p", ">"
            };

        var serviceProvider = new ServiceCollection().AddSingleton<AsyncComponent>().BuildServiceProvider();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var result = await htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<NestedAsyncComponent>(ParameterView.FromDictionary(new Dictionary<string, object>
        {
            ["Nested"] = false,
            ["Value"] = 10
        })));

        // Assert
        AssertHtmlContentEquals(expectedHtml, result.HtmlContent);
    }

    [Fact]
    public async Task PrerendersMultipleComponentsSuccessfully()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddMarkupContent(1, "<span>Hello world!</span>");
            rtb.CloseElement();
        })).BuildServiceProvider();
        var renderer = GetHtmlRenderer(serviceProvider);

        // Act
        var first = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<TestComponent>(ParameterView.Empty));
        var second = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<TestComponent>(ParameterView.Empty));

        // Assert
        Assert.Equal(0, first.ComponentId);
        Assert.Equal(1, second.ComponentId);
    }

    private HtmlRenderer GetHtmlRenderer(IServiceProvider serviceProvider)
    {
        return new HtmlRenderer(serviceProvider, NullLoggerFactory.Instance, new TestViewBufferScope());
    }

    private class NestedAsyncComponent : ComponentBase
    {
        [Parameter] public bool Nested { get; set; }
        [Parameter] public int Value { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Value = Value * 2;
            await Task.Yield();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, Value.ToString(CultureInfo.InvariantCulture));
            builder.CloseElement();
            if (!Nested)
            {
                builder.OpenComponent<NestedAsyncComponent>(2);
                builder.AddAttribute(3, "Nested", true);
                builder.AddAttribute(4, "Value", Value * 2);
                builder.CloseComponent();
            }
        }
    }

    private class AsyncComponent : ComponentBase
    {
        public AsyncComponent()
        {
        }

        [Parameter]
        public int Value { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Value = Value * 2;
            await Task.Delay(Value * 100);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, Value.ToString(CultureInfo.InvariantCulture));
            builder.CloseElement();
        }
    }

    private class ChildComponent : IComponent
    {
        private RenderHandle _renderHandle;

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            var content = parameters.GetValueOrDefault<string>("Value");
            _renderHandle.Render(CreateRenderFragment(content));
            return Task.CompletedTask;
        }

        private RenderFragment CreateRenderFragment(string content)
        {
            return RenderFragment;

            void RenderFragment(RenderTreeBuilder rtb)
            {
                rtb.OpenElement(1, "span");
                rtb.AddContent(2, content);
                rtb.CloseElement();
            }
        }
    }

    private class TestComponent : IComponent
    {
        private RenderHandle _renderHandle;

        [Inject]
        public RenderFragment Fragment { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            _renderHandle.Render(Fragment);
            return Task.CompletedTask;
        }
    }
}
