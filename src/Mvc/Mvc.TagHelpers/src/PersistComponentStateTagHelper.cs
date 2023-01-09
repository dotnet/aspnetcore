// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// A <see cref="TagHelper"/> that saves the state of Razor components rendered on the page up to that point.
/// </summary>
[HtmlTargetElement(TagHelperName, TagStructure = TagStructure.WithoutEndTag)]
public class PersistComponentStateTagHelper : TagHelper
{
    private const string TagHelperName = "persist-component-state";
    private const string PersistenceModeName = "persist-mode";
    private PersistenceMode? _persistenceMode;

    /// <summary>
    /// Gets or sets the <see cref="Rendering.ViewContext"/> for the current request.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="PersistenceMode"/> for the state to persist.
    /// </summary>
    [HtmlAttributeName(PersistenceModeName)]
    public PersistenceMode? PersistenceMode
    {
        get => _persistenceMode;
        set => _persistenceMode = value;
    }

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        var services = ViewContext.HttpContext.RequestServices;
        var manager = services.GetRequiredService<ComponentStatePersistenceManager>();
        var renderer = services.GetRequiredService<HtmlRenderer>();
        var store = PersistenceMode switch
        {
            null => ComponentRenderer.GetPersistStateRenderMode(ViewContext) switch
            {
                InvokedRenderModes.Mode.None =>
                    null,
                InvokedRenderModes.Mode.WebAssembly =>
                    new PrerenderComponentApplicationStore(),
                InvokedRenderModes.Mode.Server =>
                    new ProtectedPrerenderComponentApplicationStore(services.GetRequiredService<IDataProtectionProvider>()),
                InvokedRenderModes.Mode.ServerAndWebAssembly =>
                    throw new InvalidOperationException(
                        Resources.FormatPersistComponentStateTagHelper_FailedToInferComponentPersistenceMode(PersistenceModeName)),
                _ => throw new InvalidOperationException("Invalid InvokedRenderMode.")
            },
            TagHelpers.PersistenceMode.Server =>
                new ProtectedPrerenderComponentApplicationStore(services.GetRequiredService<IDataProtectionProvider>()),
            TagHelpers.PersistenceMode.WebAssembly =>
                new PrerenderComponentApplicationStore(),
            _ =>
                throw new InvalidOperationException("Invalid persistence mode.")
        };

        output.TagName = null;
        if (store != null)
        {
            await manager.PersistStateAsync(store, renderer);
            output.Content.SetHtmlContent(
                new HtmlContentBuilder()
                    .AppendHtml("<!--Blazor-Component-State:")
                    .AppendHtml(new ComponentStateHtmlContent(store))
                    .AppendHtml("-->"));
        }
    }

    private sealed class ComponentStateHtmlContent : IHtmlContent
    {
        private PrerenderComponentApplicationStore _store;

        public ComponentStateHtmlContent(PrerenderComponentApplicationStore store)
        {
            _store = store;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (_store != null)
            {
                writer.Write(_store.PersistedState);
                _store = null;
            }
        }
    }
}
