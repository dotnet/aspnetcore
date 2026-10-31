// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Represents properties and methods that are needed in order to render a view that uses Razor syntax.
/// </summary>
public abstract class RazorPage : RazorPageBase
{
    private readonly HashSet<string> _renderedSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool _renderedBody;
    private bool _ignoreBody;
    private HashSet<string>? _ignoredSections;

    /// <summary>
    /// An <see cref="HttpContext"/> representing the current request execution.
    /// </summary>
    public HttpContext Context => ViewContext?.HttpContext!;

    /// <summary>
    /// In a Razor layout page, renders the portion of a content page that is not within a named section.
    /// </summary>
    /// <returns>The HTML content to render.</returns>
    protected virtual IHtmlContent RenderBody()
    {
        if (BodyContent == null)
        {
            var message = Resources.FormatRazorPage_MethodCannotBeCalled(nameof(RenderBody), Path);
            throw new InvalidOperationException(message);
        }

        _renderedBody = true;
        return BodyContent;
    }

    /// <summary>
    /// In a Razor layout page, ignores rendering the portion of a content page that is not within a named section.
    /// </summary>
    public void IgnoreBody()
    {
        _ignoreBody = true;
    }

    /// <summary>
    /// Creates a named content section in the page that can be invoked in a Layout page using
    /// <see cref="RenderSection(string)"/> or <see cref="RenderSectionAsync(string, bool)"/>.
    /// </summary>
    /// <param name="name">The name of the section to create.</param>
    /// <param name="section">The <see cref="RenderAsyncDelegate"/> to execute when rendering the section.</param>
    public override void DefineSection(string name, RenderAsyncDelegate section)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(section);

        if (SectionWriters.ContainsKey(name))
        {
            throw new InvalidOperationException(Resources.FormatSectionAlreadyDefined(name));
        }
        SectionWriters[name] = section;
    }

    /// <summary>
    /// Returns a value that indicates whether the specified section is defined in the content page.
    /// </summary>
    /// <param name="name">The section name to search for.</param>
    /// <returns><c>true</c> if the specified section is defined in the content page; otherwise, <c>false</c>.</returns>
    public bool IsSectionDefined(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        EnsureMethodCanBeInvoked(nameof(IsSectionDefined));
        return PreviousSectionWriters.ContainsKey(name);
    }

    /// <summary>
    /// In layout pages, renders the content of the section named <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name of the section to render.</param>
    /// <returns>An empty <see cref="IHtmlContent"/>.</returns>
    /// <remarks>The method writes to the <see cref="RazorPageBase.Output"/> and the value returned is a token
    /// value that allows the Write (produced due to @RenderSection(..)) to succeed. However the
    /// value does not represent the rendered content.</remarks>
    public HtmlString? RenderSection(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return RenderSection(name, required: true);
    }

    /// <summary>
    /// In layout pages, renders the content of the section named <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The section to render.</param>
    /// <param name="required">Indicates if this section must be rendered.</param>
    /// <returns>An empty <see cref="IHtmlContent"/>.</returns>
    /// <remarks>The method writes to the <see cref="RazorPageBase.Output"/> and the value returned is a token
    /// value that allows the Write (produced due to @RenderSection(..)) to succeed. However the
    /// value does not represent the rendered content.</remarks>
    public HtmlString? RenderSection(string name, bool required)
    {
        ArgumentNullException.ThrowIfNull(name);

        EnsureMethodCanBeInvoked(nameof(RenderSection));

        var task = RenderSectionAsyncCore(name, required);
        return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// In layout pages, asynchronously renders the content of the section named <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The section to render.</param>
    /// <returns>
    /// A <see cref="Task{HtmlString}"/> that on completion returns an empty <see cref="IHtmlContent"/>.
    /// </returns>
    /// <remarks>The method writes to the <see cref="RazorPageBase.Output"/> and the value returned is a token
    /// value that allows the Write (produced due to @RenderSection(..)) to succeed. However the
    /// value does not represent the rendered content.</remarks>
    public Task<HtmlString?> RenderSectionAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return RenderSectionAsync(name, required: true);
    }

    /// <summary>
    /// In layout pages, asynchronously renders the content of the section named <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The section to render.</param>
    /// <param name="required">Indicates the <paramref name="name"/> section must be registered
    /// (using <c>@section</c>) in the page.</param>
    /// <returns>
    /// A <see cref="Task{HtmlString}"/> that on completion returns an empty <see cref="IHtmlContent"/>.
    /// </returns>
    /// <remarks>The method writes to the <see cref="RazorPageBase.Output"/> and the value returned is a token
    /// value that allows the Write (produced due to @RenderSection(..)) to succeed. However the
    /// value does not represent the rendered content.</remarks>
    /// <exception cref="InvalidOperationException">if <paramref name="required"/> is <c>true</c> and the section
    /// was not registered using the <c>@section</c> in the Razor page.</exception>
    public Task<HtmlString?> RenderSectionAsync(string name, bool required)
    {
        ArgumentNullException.ThrowIfNull(name);

        EnsureMethodCanBeInvoked(nameof(RenderSectionAsync));
        return RenderSectionAsyncCore(name, required);
    }

    private async Task<HtmlString?> RenderSectionAsyncCore(string sectionName, bool required)
    {
        if (_renderedSections.Contains(sectionName))
        {
            var message = Resources.FormatSectionAlreadyRendered(nameof(RenderSectionAsync), Path, sectionName);
            throw new InvalidOperationException(message);
        }

        if (PreviousSectionWriters.TryGetValue(sectionName, out var renderDelegate))
        {
            _renderedSections.Add(sectionName);

            await renderDelegate();

            // Return a token value that allows the Write call that wraps the RenderSection \ RenderSectionAsync
            // to succeed.
            return HtmlString.Empty;
        }
        else if (required)
        {
            // If the section is not found, and it is not optional, throw an error.
            var viewContext = ViewContext;
            throw new InvalidOperationException(
                Resources.FormatSectionNotDefined(
                    viewContext.ExecutingFilePath,
                    sectionName,
                    viewContext.View.Path));
        }
        else
        {
            // If the section is optional and not found, then don't do anything.
            return null;
        }
    }

    /// <summary>
    /// In layout pages, ignores rendering the content of the section named <paramref name="sectionName"/>.
    /// </summary>
    /// <param name="sectionName">The section to ignore.</param>
    public void IgnoreSection(string sectionName)
    {
        ArgumentNullException.ThrowIfNull(sectionName);

        if (PreviousSectionWriters.ContainsKey(sectionName))
        {
            if (_ignoredSections == null)
            {
                _ignoredSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            _ignoredSections.Add(sectionName);
        }
    }

    /// <inheritdoc />
    public override void EnsureRenderedBodyOrSections()
    {
        // a) all sections defined for this page are rendered.
        // b) if no sections are defined, then the body is rendered if it's available.
        if (PreviousSectionWriters != null && PreviousSectionWriters.Count > 0)
        {
            var sectionsNotRendered = PreviousSectionWriters.Keys.Except(
                _renderedSections,
                StringComparer.OrdinalIgnoreCase);

            string[] sectionsNotIgnored;
            if (_ignoredSections != null)
            {
                sectionsNotIgnored = sectionsNotRendered.Except(_ignoredSections, StringComparer.OrdinalIgnoreCase).ToArray();
            }
            else
            {
                sectionsNotIgnored = sectionsNotRendered.ToArray();
            }

            if (sectionsNotIgnored.Length > 0)
            {
                var sectionNames = string.Join(", ", sectionsNotIgnored);
                throw new InvalidOperationException(Resources.FormatSectionsNotRendered(Path, sectionNames, nameof(IgnoreSection)));
            }
        }
        else if (BodyContent != null && !_renderedBody && !_ignoreBody)
        {
            // There are no sections defined, but RenderBody was NOT called.
            // If a body was defined and the body not ignored, then RenderBody should have been called.
            var message = Resources.FormatRenderBodyNotCalled(nameof(RenderBody), Path, nameof(IgnoreBody));
            throw new InvalidOperationException(message);
        }
    }

    /// <inheritdoc/>
    public override void BeginContext(int position, int length, bool isLiteral)
    {
        // noop
    }

    /// <inheritdoc/>
    public override void EndContext()
    {
        // noop
    }

    private void EnsureMethodCanBeInvoked(string methodName)
    {
        if (PreviousSectionWriters == null)
        {
            throw new InvalidOperationException(Resources.FormatRazorPage_MethodCannotBeCalled(methodName, Path));
        }
    }
}
