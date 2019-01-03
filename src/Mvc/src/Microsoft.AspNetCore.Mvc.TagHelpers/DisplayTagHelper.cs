// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    [HtmlTargetElement("display", Attributes = "for", TagStructure = TagStructure.WithoutEndTag)]
    public class DisplayTagHelper : TagHelper
    {
        private const string ForAttributeName = "for";
        private const string AdditionalViewDataDictionaryName = "additional-view-data";
        private const string AdditionalViewDataDictionaryPrefix = "view-data-";
        private readonly IViewTemplateFactory _viewFactory;
        private readonly IViewBufferScope _bufferScope;
        private IDictionary<string, object> _additionalViewData;

        public DisplayTagHelper(
            IViewTemplateFactory viewFactory,
            IViewBufferScope bufferScope)
        {
            _viewFactory = viewFactory;
            _bufferScope = bufferScope;
        }

        /// <summary>
        /// An expression to be evaluated against the current model.
        /// </summary>
        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        public string TemplateName { get; set; }

        public string HtmlFieldName { get; set; }

        [HtmlAttributeName(AdditionalViewDataDictionaryName, DictionaryAttributePrefix = AdditionalViewDataDictionaryPrefix)]
        public IDictionary<string, object> AdditionalViewData
        {
            get
            {
                if (_additionalViewData == null)
                {
                    _additionalViewData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                }

                return _additionalViewData;
            }
            set
            {
                _additionalViewData = value;
            }
        }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Reset the TagName. We don't want `display` to render.
            output.TagName = null;

            var htmlFieldName = HtmlFieldName ?? For.Name;

            var templateBuilder = new TemplateBuilder(
                _viewFactory,
                _bufferScope,
                ViewContext,
                ViewContext.ViewData,
                For.ModelExplorer,
                htmlFieldName,
                TemplateName,
                readOnly: true,
                AdditionalViewData);

            var template = await templateBuilder.BuildAsync();
            output.Content.SetHtmlContent(template);
        }
    }
}
