// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
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
        /// Gets or sets the name used to identify this component in <c>&lt;prerender-output&gt;</c> elements.
        /// </summary>
        [HtmlAttributeName(PrerenderingHelpers.PrerenderedNameName)]
        public string Name { get; set; }

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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (_renderMode is null)
            {
                throw new InvalidOperationException(Resources.FormatAttributeIsRequired(RenderModeName, TagHelperName));
            }

            var requestServices = ViewContext.HttpContext.RequestServices;
            var componentRenderer = requestServices.GetRequiredService<IComponentRenderer>();
            var result = await componentRenderer.RenderComponentAsync(ViewContext, ComponentType, RenderMode, _parameters);

            // Reset the TagName. We don't want `component` to render.
            output.TagName = null;

            if (context.Items.ContainsKey(typeof(PrerenderSourceTagHelper)))
            {
                if (string.IsNullOrEmpty(Name))
                {
                    throw new InvalidOperationException(
                        $"Components in <{PrerenderSourceTagHelper.TagHelperName}> elements " +
                        $"must specify a '{PrerenderingHelpers.PrerenderedNameName}' attribute.");
                }

                var prerenderCache = PrerenderingHelpers.GetOrCreatePrerenderCache(ViewContext);

                if (prerenderCache.ContainsKey(Name))
                {
                    throw new InvalidOperationException(
                        $"Components in <{PrerenderSourceTagHelper.TagHelperName}> elements " +
                        $"may not have identical '{PrerenderingHelpers.PrerenderedNameName}' attributes.");
                }

                prerenderCache.Add(Name, result);
                output.Content.SetHtmlContent(string.Empty);
            }
            else 
            {
                output.Content.SetHtmlContent(result);
            }
        }
    }
}
