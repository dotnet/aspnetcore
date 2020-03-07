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
        public async override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
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
                throw new ArgumentException(Resources.FormatAttributeIsRequired(RenderModeName, TagHelperName), nameof(RenderMode));
            }

            var componentRenderer = ViewContext.HttpContext.RequestServices.GetRequiredService<IComponentRenderer>();
            var result = await componentRenderer.RenderComponentAsync(ViewContext, ComponentType, RenderMode, _parameters);

            // Reset the TagName. We don't want `component` to render.
            output.TagName = null;
            output.Content.SetHtmlContent(result);
        }
    }
}
