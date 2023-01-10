// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// A <see cref="TagHelper"/> that renders a Razor component.
/// </summary>
[HtmlTargetElement(TagHelperName, Attributes = ComponentTypeName, TagStructure = TagStructure.WithoutEndTag)]
public sealed class ComponentTagHelper : TagHelper
{
    private const string TagHelperName = "component";
    private const string ComponentParameterName = "params";
    private const string ComponentParameterPrefix = "param-";
    private const string ComponentTypeName = "type";
    private const string RenderModeName = "render-mode";
    private IDictionary<string, object> _parameters;
    private RenderMode? _renderMode;

    /// <summary>
    /// Gets or sets the <see cref="Rendering.ViewContext"/> for the current request.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Gets or sets values for component parameters.
    /// </summary>
    [HtmlAttributeName(ComponentParameterName, DictionaryAttributePrefix = ComponentParameterPrefix)]
    public IDictionary<string, object> Parameters
    {
        get
        {
            _parameters ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            return _parameters;
        }
        set => _parameters = value;
    }

    /// <summary>
    /// Gets or sets the component type. This value is required.
    /// </summary>
    [HtmlAttributeName(ComponentTypeName)]
    public Type ComponentType { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Rendering.RenderMode"/>
    /// </summary>
    [HtmlAttributeName(RenderModeName)]
    public RenderMode RenderMode
    {
        get => _renderMode ?? default;
        set
        {
            switch (value)
            {
                case RenderMode.Server:
                case RenderMode.ServerPrerendered:
                case RenderMode.Static:
                case RenderMode.WebAssembly:
                case RenderMode.WebAssemblyPrerendered:
                    _renderMode = value;
                    break;

                default:
                    throw new ArgumentException(
                        message: Resources.FormatInvalidEnumArgument(
                            nameof(value),
                            value,
                            typeof(RenderMode).FullName),
                        paramName: nameof(value));
            }
        }
    }

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (_renderMode is null)
        {
            throw new InvalidOperationException(Resources.FormatAttributeIsRequired(RenderModeName, TagHelperName));
        }

        var requestServices = ViewContext.HttpContext.RequestServices;
        var componentRenderer = requestServices.GetRequiredService<IComponentRenderer>();
        var result = await componentRenderer.RenderComponentAsync(ViewContext, ComponentType, RenderMode, _parameters);

        // Reset the TagName. We don't want `component` to render.
        output.TagName = null;
        output.Content.SetHtmlContent(result);
    }
}
