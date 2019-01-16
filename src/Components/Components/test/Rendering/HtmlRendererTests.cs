// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components.Rendering
{
    public class HtmlRendererTests
    {
        private static readonly Func<string, string> _encoder = (string t) => HtmlEncoder.Default.Encode(t);

        [Fact]
        public void RenderComponent_CanRenderEmptyElement()
        {
            // Arrange
            var expectedHtml = new[] { "<", "p", ">", "</", "p", ">" };
            var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
            {
                rtb.OpenElement(0, "p");
                rtb.CloseElement();
            })).BuildServiceProvider();
            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_CanRenderSimpleComponent()
        {
            // Arrange
            var expectedHtml = new[] { "<", "p", ">", "Hello world!", "</", "p", ">" };
            var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
            {
                rtb.OpenElement(0, "p");
                rtb.AddContent(1, "Hello world!");
                rtb.CloseElement();
            })).BuildServiceProvider();
            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_HtmlEncodesContent()
        {
            // Arrange
            var expectedHtml = new[] { "<", "p", ">", "&lt;Hello world!&gt;", "</", "p", ">" };
            var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
            {
                rtb.OpenElement(0, "p");
                rtb.AddContent(1, "<Hello world!>");
                rtb.CloseElement();
            })).BuildServiceProvider();
            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }


        [Fact]
        public void RenderComponent_DoesNotEncodeMarkup()
        {
            // Arrange
            var expectedHtml = new[] { "<", "p", ">", "<span>Hello world!</span>", "</", "p", ">" };
            var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
            {
                rtb.OpenElement(0, "p");
                rtb.AddMarkupContent(1, "<span>Hello world!</span>");
                rtb.CloseElement();
            })).BuildServiceProvider();
            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }


        [Fact]
        public void RenderComponent_CanRenderWithAttributes()
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

            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_HtmlEncodesAttributeValues()
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

            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_CanRenderBooleanAttributes()
        {
            // Arrange
            var expectedHtml = new[] { "<", "input", " ", "disabled", " />" };
            var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
            {
                rtb.OpenElement(0, "input");
                rtb.AddAttribute(1, "disabled", true);
                rtb.CloseElement();
            })).BuildServiceProvider();

            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_DoesNotRenderBooleanAttributesWhenValueIsFalse()
        {
            // Arrange
            var expectedHtml = new[] { "<", "input", " />" };
            var serviceProvider = new ServiceCollection().AddSingleton(new RenderFragment(rtb =>
            {
                rtb.OpenElement(0, "input");
                rtb.AddAttribute(1, "disabled", false);
                rtb.CloseElement();
            })).BuildServiceProvider();

            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_CanRenderWithChildren()
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

            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_CanRenderWithMultipleChildren()
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

            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_CanRenderComponentWithChildrenComponents()
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

            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_ComponentReferenceNoops()
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

            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_CanPassParameters()
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

            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);
            Action<UIChangeEventArgs> change = (UIChangeEventArgs changeArgs) => throw new InvalidOperationException();

            // Act
            var result = htmlRenderer.RenderComponent<ComponentWithParameters>(
                new ParameterCollection(new[] {
                    RenderTreeFrame.Element(0,string.Empty),
                    RenderTreeFrame.Attribute(1,"update",change),
                    RenderTreeFrame.Attribute(2,"value",5)
                }, 0));

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_CanRenderComponentWithRenderFragmentContent()
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

            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        [Fact]
        public void RenderComponent_ElementRefsNoops()
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

            var htmlRenderer = new HtmlRenderer(serviceProvider, _encoder);

            // Act
            var result = htmlRenderer.RenderComponent<TestComponent>(ParameterCollection.Empty);

            // Assert
            Assert.Equal(expectedHtml, result);
        }

        private class ComponentWithParameters : IComponent
        {
            public RenderHandle RenderHandle { get; private set; }

            public void Init(RenderHandle renderHandle)
            {
                RenderHandle = renderHandle;
            }

            [Inject]
            Func<ParameterCollection, RenderFragment> CreateRenderFragment { get; set; }

            public void SetParameters(ParameterCollection parameters)
            {
                RenderHandle.Render(CreateRenderFragment(parameters));
            }
        }

        private class ChildComponent : IComponent
        {
            private RenderHandle _renderHandle;

            public void Init(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public void SetParameters(ParameterCollection parameters)
            {
                _renderHandle.Render(CreateRenderFragment(parameters));
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

            public void Init(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public void SetParameters(ParameterCollection parameters)
            {
                _renderHandle.Render(Fragment);
            }
        }
    }
}
