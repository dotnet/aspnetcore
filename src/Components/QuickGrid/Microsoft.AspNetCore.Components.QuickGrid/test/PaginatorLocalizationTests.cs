// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

// These tests render a real <Paginator> through the shared TestRenderer (matching GridSortTest and
// GridRaceConditionTest) rather than reflecting over private members, so they exercise the actual
// localization behavior as it reaches the DOM: the embedded fallback, the app-provided
// IStringLocalizer<Paginator>, the fallback precedence rules, and the PaginationPageStatus
// placeholder rendering.
public class PaginatorLocalizationTests
{
    [Fact]
    public void RendersEmbeddedEnglish_WhenNoLocalizerRegistered()
    {
        using var _ = UseCulture(CultureInfo.InvariantCulture);

        var frames = RenderPaginator(CreateState(totalItemCount: 43, itemsPerPage: 10));

        Assert.Equal("43 items", GetElementText(frames, "summary"));
        Assert.Equal("Page 1 of 5", GetElementText(frames, "pagination-text"));
        Assert.Equal("Go to next page", GetElementAttribute(frames, "go-next", "title"));
        Assert.Equal("Go to next page", GetElementAttribute(frames, "go-next", "aria-label"));
    }

    [Fact]
    public void UsesRegisteredLocalizer_WhenResourceExists()
    {
        using var _ = UseCulture(CultureInfo.InvariantCulture);

        var localizer = new StubLocalizer(new Dictionary<string, LocalizedString>
        {
            ["Items"] = Found("Items", "elementos"),
            ["PaginationPageStatus"] = Found("PaginationPageStatus", "Página {0} de {1}"),
            ["GoToNextPage"] = Found("GoToNextPage", "Ir a la página siguiente"),
        });

        var frames = RenderPaginator(CreateState(totalItemCount: 43, itemsPerPage: 10), localizer);

        Assert.Equal("43 elementos", GetElementText(frames, "summary"));
        Assert.Equal("Página 1 de 5", GetElementText(frames, "pagination-text"));
        Assert.Equal("Ir a la página siguiente", GetElementAttribute(frames, "go-next", "title"));
        Assert.Equal("Ir a la página siguiente", GetElementAttribute(frames, "go-next", "aria-label"));
    }

    [Fact]
    public void FallsBackToEmbeddedEnglish_WhenLocalizerReportsResourceNotFound()
    {
        using var _ = UseCulture(CultureInfo.InvariantCulture);

        var localizer = new StubLocalizer(new Dictionary<string, LocalizedString>
        {
            // Present in the dictionary but flagged not-found; must not shadow the embedded resource.
            ["Items"] = new LocalizedString("Items", "should-not-be-used", resourceNotFound: true),
        });

        var frames = RenderPaginator(CreateState(totalItemCount: 43, itemsPerPage: 10), localizer);

        Assert.Equal("43 items", GetElementText(frames, "summary"));
    }

    [Fact]
    public void FallsBackToEmbeddedEnglish_WhenLocalizerReturnsEmptyValue()
    {
        using var _ = UseCulture(CultureInfo.InvariantCulture);

        var localizer = new StubLocalizer(new Dictionary<string, LocalizedString>
        {
            // A "found" but empty resource must not render as an empty label.
            ["Items"] = Found("Items", string.Empty),
        });

        var frames = RenderPaginator(CreateState(totalItemCount: 43, itemsPerPage: 10), localizer);

        Assert.Equal("43 items", GetElementText(frames, "summary"));
    }

    [Fact]
    public void RendersPaginationStatus_WithPageNumbersInStrongElements()
    {
        using var _ = UseCulture(CultureInfo.InvariantCulture);

        var frames = RenderPaginator(CreateState(totalItemCount: 43, itemsPerPage: 10));

        // {0} -> current page, {1} -> last page, each wrapped in its own <strong>.
        Assert.Equal(new[] { "1", "5" }, GetChildElementTexts(frames, "pagination-text", "strong"));
    }

    [Fact]
    public void RendersReorderedPlaceholders_WhenLocalizerReordersTemplate()
    {
        using var _ = UseCulture(CultureInfo.InvariantCulture);

        var localizer = new StubLocalizer(new Dictionary<string, LocalizedString>
        {
            ["PaginationPageStatus"] = Found("PaginationPageStatus", "{1} total pages, currently {0}"),
        });

        var frames = RenderPaginator(CreateState(totalItemCount: 43, itemsPerPage: 10), localizer);

        Assert.Equal("5 total pages, currently 1", GetElementText(frames, "pagination-text"));
        Assert.Equal(new[] { "5", "1" }, GetChildElementTexts(frames, "pagination-text", "strong"));
    }

    [Fact]
    public void RendersMalformedTemplateVerbatim_WithoutThrowing()
    {
        using var _ = UseCulture(CultureInfo.InvariantCulture);

        var localizer = new StubLocalizer(new Dictionary<string, LocalizedString>
        {
            // No {0}/{1} placeholders: the template must render verbatim rather than throwing or
            // wrapping any page numbers in <strong>.
            ["PaginationPageStatus"] = Found("PaginationPageStatus", "Page {2} of broken"),
        });

        var frames = RenderPaginator(CreateState(totalItemCount: 43, itemsPerPage: 10), localizer);

        Assert.Equal("Page {2} of broken", GetElementText(frames, "pagination-text"));
        Assert.Empty(GetChildElementTexts(frames, "pagination-text", "strong"));
    }

    private static PaginationState CreateState(int totalItemCount, int itemsPerPage)
    {
        var state = new PaginationState { ItemsPerPage = itemsPerPage };
        // QueryName is normally assigned by the owning QuickGrid; a standalone Paginator needs a
        // non-empty name or GetUriWithQueryParameter throws. Both members are internal to QuickGrid
        // and visible here via InternalsVisibleTo. SetTotalItemCountAsync completes synchronously
        // because there are no subscribers yet and the current page index (0) is within range.
        state.QueryName = "page";
        state.SetTotalItemCountAsync(totalItemCount).GetAwaiter().GetResult();
        return state;
    }

    private static ArrayRange<RenderTreeFrame> RenderPaginator(
        PaginationState state,
        IStringLocalizer<Paginator>? localizer = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        if (localizer is not null)
        {
            services.AddSingleton(localizer);
        }

        var renderer = new TestRenderer(services.BuildServiceProvider());

        // The Paginator is rendered as a *child* of a host component rather than as the root: the
        // renderer only performs [Inject] property injection on components it instantiates itself
        // (children), not on externally-created root components. Rendering it as a child mirrors how
        // GridRaceConditionTest exercises QuickGrid and how the Paginator is used in practice.
        var host = new PaginatorHost { State = state };
        var hostId = renderer.AssignRootComponentId(host);
        renderer.RenderRootComponent(hostId);

        var hostFrames = renderer.GetCurrentRenderTreeFrames(hostId);
        for (var i = 0; i < hostFrames.Count; i++)
        {
            if (hostFrames.Array[i].FrameType == RenderTreeFrameType.Component && hostFrames.Array[i].Component is Paginator)
            {
                return renderer.GetCurrentRenderTreeFrames(hostFrames.Array[i].ComponentId);
            }
        }

        throw new InvalidOperationException("The Paginator component was not rendered by the host.");
    }

    private sealed class PaginatorHost : ComponentBase
    {
        public PaginationState State { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Paginator>(0);
            builder.AddComponentParameter(1, nameof(Paginator.State), State);
            builder.CloseComponent();
        }
    }

    // Concatenates the text content of the first element carrying the given CSS class, trimming the
    // formatting whitespace Razor emits around @expressions.
    private static string GetElementText(ArrayRange<RenderTreeFrame> frames, string className)
    {
        var array = frames.Array;
        for (var i = 0; i < frames.Count; i++)
        {
            if (array[i].FrameType == RenderTreeFrameType.Element && HasClass(array, frames.Count, i, className))
            {
                var end = i + array[i].ElementSubtreeLength;
                var sb = new StringBuilder();
                for (var j = i + 1; j < end; j++)
                {
                    if (array[j].FrameType == RenderTreeFrameType.Text)
                    {
                        sb.Append(array[j].TextContent);
                    }
                }

                return sb.ToString().Trim();
            }
        }

        throw new InvalidOperationException($"No element with class '{className}' was rendered.");
    }

    // Returns the value of the named attribute on the first element carrying the given CSS class.
    private static string? GetElementAttribute(ArrayRange<RenderTreeFrame> frames, string className, string attributeName)
    {
        var array = frames.Array;
        for (var i = 0; i < frames.Count; i++)
        {
            if (array[i].FrameType == RenderTreeFrameType.Element && HasClass(array, frames.Count, i, className))
            {
                for (var j = i + 1; j < frames.Count && array[j].FrameType == RenderTreeFrameType.Attribute; j++)
                {
                    if (array[j].AttributeName == attributeName)
                    {
                        return array[j].AttributeValue as string;
                    }
                }
            }
        }

        throw new InvalidOperationException($"No element with class '{className}' was rendered.");
    }

    // Returns the text content of each child element with the given name inside the first element
    // carrying the given CSS class (e.g. every <strong> inside .pagination-text).
    private static List<string> GetChildElementTexts(
        ArrayRange<RenderTreeFrame> frames,
        string containerClass,
        string childElementName)
    {
        var array = frames.Array;
        var result = new List<string>();
        for (var i = 0; i < frames.Count; i++)
        {
            if (array[i].FrameType == RenderTreeFrameType.Element && HasClass(array, frames.Count, i, containerClass))
            {
                var end = i + array[i].ElementSubtreeLength;
                for (var j = i + 1; j < end; j++)
                {
                    if (array[j].FrameType == RenderTreeFrameType.Element && array[j].ElementName == childElementName)
                    {
                        var childEnd = j + array[j].ElementSubtreeLength;
                        var sb = new StringBuilder();
                        for (var k = j + 1; k < childEnd; k++)
                        {
                            if (array[k].FrameType == RenderTreeFrameType.Text)
                            {
                                sb.Append(array[k].TextContent);
                            }
                        }

                        result.Add(sb.ToString());
                    }
                }

                break;
            }
        }

        return result;
    }

    private static bool HasClass(RenderTreeFrame[] array, int count, int elementIndex, string className)
    {
        for (var j = elementIndex + 1; j < count && array[j].FrameType == RenderTreeFrameType.Attribute; j++)
        {
            if (array[j].AttributeName == "class" && (array[j].AttributeValue as string) == className)
            {
                return true;
            }
        }

        return false;
    }

    private static LocalizedString Found(string name, string value)
        => new(name, value, resourceNotFound: false);

    private static IDisposable UseCulture(CultureInfo culture) => new CultureScope(culture);

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture;
        private readonly CultureInfo _uiCulture;

        public CultureScope(CultureInfo culture)
        {
            _culture = CultureInfo.CurrentCulture;
            _uiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }
    }

    private sealed class StubLocalizer : IStringLocalizer<Paginator>
    {
        private readonly IReadOnlyDictionary<string, LocalizedString> _values;

        public StubLocalizer(IReadOnlyDictionary<string, LocalizedString> values)
        {
            _values = values;
        }

        public LocalizedString this[string name]
            => _values.TryGetValue(name, out var value)
                ? value
                : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments]
            => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => _values.Values;
    }
}