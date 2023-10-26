// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Sections;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.HtmlRendering;

public class HtmlRendererTest
{
    [Fact]
    public async Task RenderComponentAsync_ThrowsIfNotOnSyncContext()
    {
        // Arrange
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(_ => { })));
        var htmlRenderer = GetHtmlRenderer(serviceProvider);

        // Act
        var resultTask = htmlRenderer.RenderComponentAsync<TestComponent>();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => resultTask);
        Assert.Contains("The current thread is not associated with the Dispatcher", ex.Message);
    }

    [Fact]
    public async Task HtmlContent_Write_ThrowsIfNotOnSyncContext()
    {
        // Arrange
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(_ => { })));
        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        var htmlContent = await htmlRenderer.Dispatcher.InvokeAsync(htmlRenderer.BeginRenderingComponent<TestComponent>);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => htmlContent.WriteHtmlTo(new StringWriter()));
        Assert.Contains("The current thread is not associated with the Dispatcher", ex.Message);
    }

    [Fact]
    public async Task RenderComponentAsync_CanRenderEmptyElement()
    {
        // Arrange
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            Assert.Equal("<p></p>", result.ToHtmlString());
        });
    }

    [Fact]
    public async Task RenderComponentAsync_CanRenderSimpleComponent()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", ">", "Hello world!", "</", "p", ">" };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddContent(1, "Hello world!");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_HtmlEncodesContent()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", ">", "&lt;Hello world!&gt;", "</", "p", ">" };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddContent(1, "<Hello world!>");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_DoesNotEncodeMarkup()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", ">", "<span>Hello world!</span>", "</", "p", ">" };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddMarkupContent(1, "<span>Hello world!</span>");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_CanRenderWithAttributes()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", " ", "class", "=", "\"", "lead", "\"", ">", "Hello world!", "</", "p", ">" };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddAttribute(1, "class", "lead");
            rtb.AddContent(2, "Hello world!");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_SkipsDuplicatedAttribute()
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
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddAttribute(1, "class", "test1");
            rtb.AddAttribute(2, "another", "another-value");
            rtb.AddMultipleAttributes(3, new Dictionary<string, object>() { { "Class", "test2" }, });
            rtb.AddContent(4, "Hello world!");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_HtmlEncodesAttributeValues()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", " ", "class", "=", "\"", "&lt;lead", "\"", ">", "Hello world!", "</", "p", ">" };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddAttribute(1, "class", "<lead");
            rtb.AddContent(2, "Hello world!");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {

            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_CanRenderBooleanAttributes()
    {
        // Arrange
        var expectedHtml = new[] { "<", "input", " ", "disabled", " />" };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "input");
            rtb.AddAttribute(1, "disabled", true);
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_DoesNotRenderBooleanAttributesWhenValueIsFalse()
    {
        // Arrange
        var expectedHtml = new[] { "<", "input", " />" };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "input");
            rtb.AddAttribute(1, "disabled", false);
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_CanRenderWithChildren()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", ">", "<", "span", ">", "Hello world!", "</", "span", ">", "</", "p", ">" };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.OpenElement(1, "span");
            rtb.AddContent(2, "Hello world!");
            rtb.CloseElement();
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_CanRenderWithMultipleChildren()
    {
        // Arrange
        var expectedHtml = new[] { "<", "p", ">",
            "<", "span", ">", "Hello world!", "</", "span", ">",
            "<", "span", ">", "Bye Bye world!", "</", "span", ">",
            "</", "p", ">"
        };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.OpenElement(1, "span");
            rtb.AddContent(2, "Hello world!");
            rtb.CloseElement();
            rtb.OpenElement(3, "span");
            rtb.AddContent(4, "Bye Bye world!");
            rtb.CloseElement();
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_MarksSelectedOptionsAsSelected()
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
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
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
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_RendersValueAttributeAsTextContentOfTextareaElement()
    {
        // Arrange
        var expectedHtml = "<textarea rows=\"10\" cols=\"20\">Hello &lt;html&gt;-encoded content!</textarea>";
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "textarea");
            rtb.AddAttribute(1, "value", "Hello <html>-encoded content!");
            rtb.AddAttribute(2, "rows", "10");
            rtb.AddAttribute(3, "cols", "20");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_RendersTextareaElementWithoutValueAttribute()
    {
        // Arrange
        var expectedHtml = "<textarea rows=\"10\" cols=\"20\">Hello &lt;html&gt;-encoded content!</textarea>";
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "textarea");
            rtb.AddAttribute(1, "rows", "10");
            rtb.AddAttribute(2, "cols", "20");
            rtb.AddContent(3, "Hello <html>-encoded content!");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_RendersTextareaElementWithoutValueAttributeOrTextContent()
    {
        // Arrange
        var expectedHtml = "<textarea rows=\"10\" cols=\"20\"></textarea>";
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "textarea");
            rtb.AddAttribute(1, "rows", "10");
            rtb.AddAttribute(2, "cols", "20");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_ValueAttributeOfTextareaElementOverridesTextContent()
    {
        // Arrange
        var expectedHtml = "<textarea>Hello World!</textarea>";
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "textarea");
            rtb.AddAttribute(1, "value", "Hello World!");
            rtb.AddContent(3, "Some content");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_RendersSelfClosingElement()
    {
        // Arrange
        var expectedHtml = "<input value=\"Hello &lt;html&gt;-encoded content!\" id=\"Test\" />";
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "input");
            rtb.AddAttribute(1, "value", "Hello <html>-encoded content!");
            rtb.AddAttribute(2, "id", "Test");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_RendersSelfClosingElementWithTextComponentAsNormalElement()
    {
        // Arrange
        var expectedHtml = "<meta>Something</meta>";
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "meta");
            rtb.AddContent(1, "Something");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_RendersSelfClosingElementBySkippingElementReferenceCapture()
    {
        // Arrange
        var expectedHtml = "<input value=\"Hello &lt;html&gt;-encoded content!\" id=\"Test\" />";
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "input");
            rtb.AddAttribute(1, "value", "Hello <html>-encoded content!");
            rtb.AddAttribute(2, "id", "Test");
            rtb.AddElementReferenceCapture(3, inputReference => _ = inputReference);
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_MarksSelectedOptionsAsSelected_WithOptGroups()
    {
        // Arrange
        var expectedHtml =
            @"<select value=""beta"">" +
            @"<optgroup><option value=""alpha"">alpha</option></optgroup>" +
            @"<optgroup><option value=""beta"" selected>beta</option></optgroup>" +
            @"<optgroup><option value=""gamma"">gamma</option></optgroup>" +
            "</select>";
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
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
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_CanRenderComponentAsyncWithChildrenComponents()
    {
        // Arrange
        var expectedHtml = new[] {
                "<", "p", ">", "<", "span", ">", "Hello world!", "</", "span", ">", "</", "p", ">",
                "<", "span", ">", "Child content!", "</", "span", ">"
            };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.OpenElement(1, "span");
            rtb.AddContent(2, "Hello world!");
            rtb.CloseElement();
            rtb.CloseElement();
            rtb.OpenComponent(3, typeof(ChildComponent));
            rtb.AddAttribute(4, "Value", "Child content!");
            rtb.CloseComponent();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_ComponentReferenceNoops()
    {
        // Arrange
        var expectedHtml = new[] {
                "<", "p", ">", "<", "span", ">", "Hello world!", "</", "span", ">", "</", "p", ">",
                "<", "span", ">", "Child content!", "</", "span", ">"
            };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
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
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_CanPassParameters()
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

        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new Func<ParameterView, RenderFragment>(Content)));
        Action<ChangeEventArgs> change = (ChangeEventArgs changeArgs) => throw new InvalidOperationException();

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<ComponentWithParameters>(
                ParameterView.FromDictionary(new Dictionary<string, object>
                {
                    { "update", change },
                    { "value", 5 }
                }));

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_CanRenderComponentAsyncWithRenderFragmentContent()
    {
        // Arrange
        var expectedHtml = new[] {
                "<", "p", ">", "<", "span", ">", "Hello world!", "</", "span", ">", "</", "p", ">" };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.OpenElement(1, "span");
            rtb.AddContent(2, rf => rf.AddContent(0, "Hello world!"));
            rtb.CloseElement();
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_ElementRefsNoops()
    {
        // Arrange
        var expectedHtml = new[]
        {
            "<", "p", ">", "<", "span", ">", "Hello world!", "</", "span", ">", "</", "p", ">"
        };
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "p");
            rtb.AddElementReferenceCapture(1, er => { });
            rtb.OpenElement(2, "span");
            rtb.AddContent(3, rf => rf.AddContent(0, "Hello world!"));
            rtb.CloseElement();
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
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
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton<AsyncComponent>());

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<AsyncComponent>(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                ["Value"] = 10
            }));

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
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

        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton<AsyncComponent>());

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<NestedAsyncComponent>(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                ["Nested"] = false,
                ["Value"] = 10
            }));

            // Assert
            AssertHtmlContentEquals(expectedHtml, result);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_CanCauseRerenderingOfEarlierComponents()
    {
        // This scenario is important when there are multiple root components. The default project
        // template relies on this - HeadOutlet re-renders when a later PageTitle component is rendered,
        // even though they are not within the same root component.

        var htmlRenderer = GetHtmlRenderer();
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Arrange/Act/Assert 1: initially get some empty output
            var first = await htmlRenderer.RenderComponentAsync<SectionOutlet>(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(SectionOutlet.SectionId), "testsection" }
            }));

            Assert.Empty(first.ToHtmlString());

            // Act/Assert 2: cause it to be updated
            var second = await htmlRenderer.RenderComponentAsync<SectionContent>(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(SectionContent.SectionId), "testsection" },
                { nameof(SectionContent.ChildContent), (RenderFragment)(builder =>
                    {
                        builder.AddContent(0, "Hello from the section content provider");
                    })
                }
            }));

            Assert.Empty(second.ToHtmlString());
            Assert.Equal("Hello from the section content provider", first.ToHtmlString());
        });
    }

    [Fact]
    public async Task RenderComponentAsync_CanOutputToTextWriter()
    {
        // Arrange
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(builder =>
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "Hey!");
            builder.CloseElement();
        })));
        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, new UTF8Encoding(false));

        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();
            result.WriteHtmlTo(writer);
            writer.Flush();

            // Assert
            var actual = Encoding.UTF8.GetString(ms.ToArray());
            Assert.Equal("<p>Hey!</p>", actual);
        });
    }

    [Fact]
    public async Task BeginRenderingComponent_CanObserveStateBeforeAndAfterQuiescence()
    {
        // Arrange
        var completionTcs = new TaskCompletionSource();
        var services = GetServiceProvider(collection =>collection.AddSingleton(new AsyncLoadingComponentCompletion { Task = completionTcs.Task }));

        var htmlRenderer = GetHtmlRenderer(services);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act/Assert: state before quiescence
            var result = htmlRenderer.BeginRenderingComponent<AsyncLoadingComponent>();
            var quiescenceTask = result.QuiescenceTask;
            Assert.False(quiescenceTask.IsCompleted);
            Assert.Equal("Loading...", result.ToHtmlString());

            // Act/Assert: state after quiescence
            completionTcs.SetResult();
            await quiescenceTask;
            Assert.Equal("Finished loading", result.ToHtmlString());
        });
    }

    [Fact]
    public async Task RenderComponentAsync_ThrowsSync()
    {
        // Arrange
        var services = GetServiceProvider(collection => collection.AddSingleton(new AsyncLoadingComponentCompletion { Task = new TaskCompletionSource().Task }));

        var htmlRenderer = GetHtmlRenderer(services);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act/Assert
            var ex = await Assert.ThrowsAsync<InvalidTimeZoneException>(async () =>
            {
                await htmlRenderer.RenderComponentAsync<ErrorThrowingComponent>(ParameterView.FromDictionary(new Dictionary<string, object>
                {
                    { nameof(ErrorThrowingComponent.ThrowSync), true }
                }));
            });
            Assert.Equal("sync", ex.Message);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_ThrowsAsync()
    {
        // Arrange
        var completionTcs = new TaskCompletionSource();
        var services = GetServiceProvider(collection => collection.AddSingleton(new AsyncLoadingComponentCompletion { Task = Task.Delay(0) }));

        var htmlRenderer = GetHtmlRenderer(services);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act/Assert
            var ex = await Assert.ThrowsAsync<InvalidTimeZoneException>(() =>
                htmlRenderer.RenderComponentAsync<ErrorThrowingComponent>(ParameterView.FromDictionary(new Dictionary<string, object>
                {
                    { nameof(ErrorThrowingComponent.ThrowAsync), true }
                })));
            Assert.Equal("async", ex.Message);
        });
    }

    [Fact]
    public async Task BeginRenderingComponent_ThrowsSync()
    {
        // Arrange
        var services = GetServiceProvider(collection => collection.AddSingleton(new AsyncLoadingComponentCompletion { Task = new TaskCompletionSource().Task }));

        var htmlRenderer = GetHtmlRenderer(services);
        await htmlRenderer.Dispatcher.InvokeAsync(() =>
        {
            // Act/Assert
            var ex = Assert.Throws<InvalidTimeZoneException>(() =>
            {
                htmlRenderer.BeginRenderingComponent<ErrorThrowingComponent>(ParameterView.FromDictionary(new Dictionary<string, object>
                {
                    { nameof(ErrorThrowingComponent.ThrowSync), true }
                }));
            });
            Assert.Equal("sync", ex.Message);
        });
    }

    [Fact]
    public async Task BeginRenderingComponent_ThrowsAsyncDuringWaitForQuiescenceAsync()
    {
        // Arrange
        var completionTcs = new TaskCompletionSource();
        var services = GetServiceProvider(collection => collection.AddSingleton(new AsyncLoadingComponentCompletion { Task = completionTcs.Task }));

        var htmlRenderer = GetHtmlRenderer(services);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act/Assert
            var content = htmlRenderer.BeginRenderingComponent<ErrorThrowingComponent>(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(ErrorThrowingComponent.ThrowAsync), true }
            }));

            var ex = await Assert.ThrowsAsync<InvalidTimeZoneException>(() =>
            {
                completionTcs.SetResult();
                return content.QuiescenceTask;
            });
            Assert.Equal("async", ex.Message);
        });
    }

    [Fact]
    public async Task RenderComponentAsync_CanRenderScriptTag_WithJavaScriptEncodedContent()
    {
        // This is equivalent to Razor markup similar to:
        //
        //     <script>
        //         alert('Hello, @name!');
        //     </script>
        //     And now with HTML encoding: @name
        //
        // Currently some extra linebreaks are needed to work around a Razor compiler issue (otherwise
        // everything is treated as content to be encoded) but once https://github.com/dotnet/razor/issues/9204
        // is fixed, the above should be correct.

        // Arrange
        var name = "Person with special chars like ' \" </script>";
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "script");
            rtb.AddMarkupContent(1, "\n    alert('Hello, ");
            rtb.AddContent(2, name);
            rtb.AddMarkupContent(3, "!');\n");
            rtb.CloseElement();
            rtb.AddMarkupContent(4, "\nAnd now with HTML encoding: ");
            rtb.AddContent(5, name);
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            Assert.Equal(@"<script>
    alert('Hello, Person with special chars like \u0027 \u0022 \u003C/script\u003E!');
</script>
And now with HTML encoding: Person with special chars like &#x27; &quot; &lt;/script&gt;".Replace("\r", ""), result.ToHtmlString());
        });
    }

    [Fact]
    public async Task RenderComponentAsync_IgnoresNamedEvents()
    {
        // Arrange
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "div");
            rtb.AddNamedEvent("someevent", "somename");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            Assert.Equal("<div></div>", result.ToHtmlString());
        });
    }

    [Fact]
    public async Task RenderComponentAsync_DoesNotAddHiddenInputForNamedSubmitEvents_WithoutFormMappingScope()
    {
        // Arrange
        var formValueMapper = new TestFormValueMapper();
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "form");
            rtb.AddNamedEvent("onsubmit", "somename");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            Assert.Equal("<form></form>", result.ToHtmlString());
        });
    }

    [Fact]
    public async Task RenderComponentAsync_AddsHiddenInputForNamedSubmitEvents_WithDefaultFormMappingContext()
    {
        // Arrange
        var formValueMapper = new TestFormValueMapper();
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "form");
            rtb.AddNamedEvent("onsubmit", "some <name>");
            rtb.CloseElement();
        }))
            .AddSingleton<ICascadingValueSupplier>(new SupplyParameterFromFormValueProvider(formValueMapper, ""))
            .AddSingleton<IFormValueMapper>(formValueMapper));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            Assert.Equal("<form><input type=\"hidden\" name=\"_handler\" value=\"some &lt;name&gt;\" /></form>", result.ToHtmlString());
        });
    }

    [Fact]
    public async Task RenderComponentAsync_AddsHiddenInputForNamedSubmitEvents_InsideNamedFormMappingScope()
    {
        // Arrange
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenComponent<FormMappingScope>(0);
            rtb.AddComponentParameter(1, nameof(FormMappingScope.Name), "myscope");
            rtb.AddComponentParameter(1, nameof(FormMappingScope.ChildContent), (RenderFragment<FormMappingContext>)(ctx => rtb =>
            {
                rtb.OpenElement(0, "form");
                rtb.AddNamedEvent("onsubmit", "somename");
                rtb.CloseElement();
            }));
            rtb.CloseComponent();
        })).AddSingleton<IFormValueMapper, TestFormValueMapper>());

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            Assert.Equal("<form><input type=\"hidden\" name=\"_handler\" value=\"[myscope]somename\" /></form>", result.ToHtmlString());
        });
    }

    [Theory]
    [InlineData("https://example.com/", "https://example.com", "/")]
    [InlineData("https://example.com/", "https://example.com/", "/")]
    [InlineData("https://example.com/", "https://example.com/page", "/page")]
    [InlineData("https://example.com/", "https://example.com/a/b/c", "/a/b/c")]
    [InlineData("https://example.com/", "https://example.com/a/b/c?q=1&p=hello%20there", "/a/b/c?q=1&p=hello%20there")]
    [InlineData("https://example.com/subdir/", "https://example.com/subdir", "/subdir")]
    [InlineData("https://example.com/subdir/", "https://example.com/subdir/", "/subdir/")]
    [InlineData("https://example.com/a/b/", "https://example.com/a/b/c?q=1&p=2", "/a/b/c?q=1&p=2")]
    [InlineData("http://user:pass@xyz.example.com:1234/a/b/", "http://user:pass@xyz.example.com:1234/a/b/c&q=1&p=2", "/a/b/c&q=1&p=2")]
    public async Task RenderComponentAsync_AddsActionAttributeWithCurrentUrlToFormWithoutAttributes_WhenNoActionSpecified(
        string baseUrl, string currentUrl, string expectedAction)
    {
        // Arrange
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "form");
            rtb.CloseElement();
        })).AddScoped<NavigationManager>(_ => new TestNavigationManager(baseUrl, currentUrl)));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            Assert.Equal($"<form action=\"{HttpUtility.HtmlAttributeEncode(expectedAction)}\"></form>", result.ToHtmlString());
        });
    }

    [Fact]
    public async Task RenderComponentAsync_AddsActionAttributeWithCurrentUrlToFormWithAttributes_WhenNoActionSpecified()
    {
        // Arrange
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "form");
            rtb.AddAttribute(1, "method", "post");
            rtb.CloseElement();
        })).AddScoped<NavigationManager, TestNavigationManager>());

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            Assert.Equal("<form method=\"post\" action=\"/page\"></form>", result.ToHtmlString());
        });
    }

    [Fact]
    public async Task RenderComponentAsync_DoesNotAddActionAttributeWithCurrentUrl_WhenActionIsExplicitlySpecified()
    {
        // Arrange
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "form");
            rtb.AddAttribute(1, "action", "https://example.com/explicit");
            rtb.CloseElement();
        })).AddScoped<NavigationManager, TestNavigationManager>());

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            Assert.Equal("<form action=\"https://example.com/explicit\"></form>", result.ToHtmlString());
        });
    }

    [Fact]
    public async Task RenderComponentAsync_DoesNotAddActionAttributeWithCurrentUrl_WhenNoNavigationManagerIsRegistered()
    {
        // Arrange
        var serviceProvider = GetServiceProvider(collection => collection.AddSingleton(new RenderFragment(rtb =>
        {
            rtb.OpenElement(0, "form");
            rtb.CloseElement();
        })));

        var htmlRenderer = GetHtmlRenderer(serviceProvider);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            // Act
            var result = await htmlRenderer.RenderComponentAsync<TestComponent>();

            // Assert
            Assert.Equal("<form></form>", result.ToHtmlString());
        });
    }

    // TODO: As above, but inside a FormMappingScope, showing its name also shows up

    void AssertHtmlContentEquals(IEnumerable<string> expected, HtmlRootComponent actual)
        => AssertHtmlContentEquals(string.Join(string.Empty, expected), actual);

    void AssertHtmlContentEquals(string expected, HtmlRootComponent actual)
    {
        var actualHtml = actual.ToHtmlString();
        Assert.Equal(expected, actualHtml);
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

    private class AsyncLoadingComponent : ComponentBase
    {
        string status;

        [Inject]
        public AsyncLoadingComponentCompletion Completion { get; set; }

        protected override async Task OnInitializedAsync()
        {
            status = "Loading...";
            await Completion.Task;
            await Task.Yield();
            // So that the test has to await the quiescence task to observe the final outcome
            status = "Finished loading";
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => builder.AddContent(0, status);
    }

    private class ErrorThrowingComponent : ComponentBase
    {
        [Parameter] public bool ThrowSync { get; set; }
        [Parameter] public bool ThrowAsync { get; set; }

        [Inject]
        public AsyncLoadingComponentCompletion Completion { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await Completion.Task;
            await Task.Yield();

            if (ThrowAsync)
            {
                throw new InvalidTimeZoneException("async");
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Hello");

            if (ThrowSync)
            {
                throw new InvalidTimeZoneException("sync");
            }

            builder.AddContent(1, "Goodbye");
        }
    }

    private class AsyncLoadingComponentCompletion
    {
        public Task Task { get; init; }
    }

    HtmlRenderer GetHtmlRenderer(IServiceProvider serviceProvider = null)
    {
        if (serviceProvider is null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped<NavigationManager, TestNavigationManager>();

            serviceProvider = services.BuildServiceProvider();
        }

        return new HtmlRenderer(serviceProvider, NullLoggerFactory.Instance);
    }

    class TestFormValueMapper : IFormValueMapper
    {
        public bool CanMap(Type valueType, string mappingScopeName, string formName)
            => throw new NotImplementedException();

        public void Map(FormValueMappingContext context)
            => throw new NotImplementedException();
    }

    private class TestNavigationManager : NavigationManager
    {
        private string _baseUrl;
        private string _currentUrl;

        public TestNavigationManager()
            : this("https://www.example.com/", "https://www.example.com/page")
        {
        }

        public TestNavigationManager(string baseUrl, string currentUrl)
        {
            _baseUrl = baseUrl;
            _currentUrl = currentUrl;
        }

        protected override void EnsureInitialized() => Initialize(_baseUrl, _currentUrl);
    }

    private IServiceProvider GetServiceProvider(Action<IServiceCollection> configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
