// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components.Rendering
{
    public abstract class HtmlRendererTestBase
    {
        protected readonly Func<string, string> _encoder = (string t) => HtmlEncoder.Default.Encode(t);
        protected readonly Dispatcher Dispatcher = Renderer.CreateDefaultDispatcher();

        protected abstract HtmlRenderer GetHtmlRenderer(IServiceProvider serviceProvider);

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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponentAsync_CanRenderSimpleComponent()
        {
            // Arrange
            var dispatcher = Renderer.CreateDefaultDispatcher();
            var expectedHtml = new[] { "<", "p", ">", "Hello world!", "</", "p", ">" };
            var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
            {
                rtb.OpenElement(0, "p");
                rtb.AddContent(1, "Hello world!");
                rtb.CloseElement();
            })).BuildServiceProvider();
            var htmlRenderer = GetHtmlRenderer(serviceProvider);

            // Act
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponentAsync_HtmlEncodesContent()
        {
            // Arrange
            var dispatcher = Renderer.CreateDefaultDispatcher();
            var expectedHtml = new[] { "<", "p", ">", "&lt;Hello world!&gt;", "</", "p", ">" };
            var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
            {
                rtb.OpenElement(0, "p");
                rtb.AddContent(1, "<Hello world!>");
                rtb.CloseElement();
            })).BuildServiceProvider();
            var htmlRenderer = GetHtmlRenderer(serviceProvider);

            // Act
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponentAsync_CanPassParameters()
        {
            // Arrange
            var expectedHtml = new[] {
                "<", "p", ">", "<", "input", " ", "value", "=", "\"", "5", "\"", " />", "</", "p", ">" };

            RenderFragment Content(ParameterCollection pc) => new RenderFragment((RenderTreeBuilder rtb) =>
            {
                rtb.OpenElement(0, "p");
                rtb.OpenElement(1, "input");
                rtb.AddAttribute(2, "change", pc.GetValueOrDefault<Action<UIChangeEventArgs>>("update"));
                rtb.AddAttribute(3, "value", pc.GetValueOrDefault<int>("value"));
                rtb.CloseElement();
                rtb.CloseElement();
            });

            var serviceProvider = new ServiceCollection()
                .AddSingleton(new Func<ParameterCollection, RenderFragment>(Content))
                .BuildServiceProvider();

            var htmlRenderer = GetHtmlRenderer(serviceProvider);
            Action<UIChangeEventArgs> change = (UIChangeEventArgs changeArgs) => throw new InvalidOperationException();

            // Act
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<ComponentWithParameters>(
                new ParameterCollection(new[] {
                    RenderTreeFrame.Element(0,string.Empty),
                    RenderTreeFrame.Attribute(1,"update",change),
                    RenderTreeFrame.Attribute(2,"value",5)
                }, 0))));

            // Assert
            Assert.Equal(expectedHtml, result);
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponentAsync_ElementRefsNoops()
        {
            // Arrange
            var expectedHtml = new[] {
                "<", "p", ">", "<", "span", ">", "Hello world!", "</", "span", ">", "</", "p", ">" };
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
            var result = GetResult(Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TestComponent>(ParameterCollection.Empty)));

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        private IEnumerable<string> GetResult(Task<ComponentRenderedText> task)
        {
            Assert.True(task.IsCompleted);
            if (task.IsCompletedSuccessfully)
            {
                return task.Result.Tokens;
            }
            else
            {
                ExceptionDispatchInfo.Capture(task.Exception).Throw();
                throw new InvalidOperationException("We will never hit this line");
            }
        }

        private class ComponentWithParameters : IComponent
        {
            public RenderHandle RenderHandle { get; private set; }

            public void Configure(RenderHandle renderHandle)
            {
                RenderHandle = renderHandle;
            }

            [Inject]
            Func<ParameterCollection, RenderFragment> CreateRenderFragment { get; set; }

            public Task SetParametersAsync(ParameterCollection parameters)
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
            var result = await Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<AsyncComponent>(ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                ["Value"] = 10
            })));

            // Assert
            Assert.Equal(expectedHtml, result.Tokens);
        }

        [Fact]
        public async Task CanRender_NestedAsyncComponents()
        {
            // Arrange
            var dispatcher = Renderer.CreateDefaultDispatcher();
            var expectedHtml = new[] {
                "<", "p", ">", "20", "</", "p", ">",
                "<", "p", ">", "80", "</", "p", ">"
            };

            var serviceProvider = new ServiceCollection().AddSingleton<AsyncComponent>().BuildServiceProvider();

            var htmlRenderer = GetHtmlRenderer(serviceProvider);

            // Act
            var result = await Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<NestedAsyncComponent>(ParameterCollection.FromDictionary(new Dictionary<string, object>
            {
                ["Nested"] = false,
                ["Value"] = 10
            })));

            // Assert
            Assert.Equal(expectedHtml, result.Tokens);
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
                builder.AddContent(1, Value.ToString());
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
                builder.AddContent(1, Value.ToString());
                builder.CloseElement();
            }
        }

        private class ChildComponent : IComponent
        {
            private RenderHandle _renderHandle;

            public void Configure(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public Task SetParametersAsync(ParameterCollection parameters)
            {
                _renderHandle.Render(CreateRenderFragment(parameters));
                return Task.CompletedTask;
            }

            private RenderFragment CreateRenderFragment(ParameterCollection parameters)
            {
                return RenderFragment;

                void RenderFragment(RenderTreeBuilder rtb)
                {
                    rtb.OpenElement(1, "span");
                    rtb.AddContent(2, parameters.GetValueOrDefault<string>("Value"));
                    rtb.CloseElement();
                }
            }
        }

        private class TestComponent : IComponent
        {
            private RenderHandle _renderHandle;

            [Inject]
            public RenderFragment Fragment { get; set; }

            public void Configure(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public Task SetParametersAsync(ParameterCollection parameters)
            {
                _renderHandle.Render(Fragment);
                return Task.CompletedTask;
            }
        }
    }
}
