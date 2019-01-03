// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// Renders a partial view.
    /// </summary>
    [HtmlTargetElement("partial", Attributes = "name", TagStructure = TagStructure.WithoutEndTag)]
    public class PartialTagHelper : TagHelper
    {
        private const string ForAttributeName = "for";
        private const string ModelAttributeName = "model";
        private const string FallbackAttributeName = "fallback-name";
        private const string OptionalAttributeName = "optional";
        private object _model;
        private bool _hasModel;
        private bool _hasFor;
        private ModelExpression _for;

        private readonly IViewTemplateFactory _viewTemplateFactory;
        private readonly IViewBufferScope _viewBufferScope;

        public PartialTagHelper(
            IViewTemplateFactory viewTemplateFactory,
            IViewBufferScope viewBufferScope)
        {
            _viewTemplateFactory = viewTemplateFactory ?? throw new ArgumentNullException(nameof(viewTemplateFactory));
            _viewBufferScope = viewBufferScope ?? throw new ArgumentNullException(nameof(viewBufferScope));
        }

        /// <summary>
        /// The name or path of the partial view that is rendered to the response.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// An expression to be evaluated against the current model. Cannot be used together with <see cref="Model"/>.
        /// </summary>
        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For
        {
            get => _for;
            set
            {
                _for = value ?? throw new ArgumentNullException(nameof(value));
                _hasFor = true;
            }
        }

        /// <summary>
        /// The model to pass into the partial view. Cannot be used together with <see cref="For"/>.
        /// </summary>
        [HtmlAttributeName(ModelAttributeName)]
        public object Model
        {
            get => _model;
            set
            {
                _model = value;
                _hasModel = true;
            }
        }

        /// <summary>
        /// When optional, executing the tag helper will no-op if the view cannot be located. 
        /// Otherwise will throw stating the view could not be found.
        /// </summary>
        [HtmlAttributeName(OptionalAttributeName)]
        public bool Optional { get; set; }

        /// <summary>
        /// View to lookup if the view specified by <see cref="Name"/> cannot be located.
        /// </summary>
        [HtmlAttributeName(FallbackAttributeName)]
        public string FallbackName { get; set; }

        /// <summary>
        /// A <see cref="ViewDataDictionary"/> to pass into the partial view.
        /// </summary>
        public ViewDataDictionary ViewData { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <inheritdoc />
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

            // Reset the TagName. We don't want `partial` to render.
            output.TagName = null;

            var viewFactoryContext = new ViewFactoryContext(ViewContext, ViewContext.ExecutingFilePath, Name, isMainPage: false);
            var result = await _viewTemplateFactory.LocateViewAsync(viewFactoryContext);

            var viewSearchedLocations = result.SearchedLocations;

            if (!result.Success && !string.IsNullOrEmpty(FallbackName))
            {
                viewFactoryContext = new ViewFactoryContext(ViewContext, ViewContext.ExecutingFilePath, FallbackName, isMainPage: false);
                result = await _viewTemplateFactory.LocateViewAsync(viewFactoryContext);
            }

            if (!result.Success)
            {
                if (Optional)
                {
                    // Could not find the view or fallback view, but the partial is marked as optional.
                    return;
                }

                var locations = Environment.NewLine + string.Join(Environment.NewLine, viewSearchedLocations);
                var errorMessage = Resources.FormatViewEngine_PartialViewNotFound(Name, locations);

                if (!string.IsNullOrEmpty(FallbackName))
                {
                    locations = Environment.NewLine + string.Join(Environment.NewLine, result.SearchedLocations);
                    errorMessage += Environment.NewLine + Resources.FormatViewEngine_FallbackViewNotFound(FallbackName, locations);
                }

                throw new InvalidOperationException(errorMessage);
            }

            var model = ResolveModel();
            var viewBuffer = new ViewBuffer(_viewBufferScope, result.ViewName, ViewBuffer.PartialViewPageSize);
            using (var writer = new ViewBufferTextWriter(viewBuffer, Encoding.UTF8))
            {
                await RenderPartialViewAsync(writer, model, result.ViewTemplate);
                output.Content.SetHtmlContent(viewBuffer);
            }
        }

        // Internal for testing
        internal object ResolveModel()
        {
            // 1. Disallow specifying values for both Model and For
            // 2. If a Model was assigned, use it even if it's null.
            // 3. For cannot have a null value. Use it if it was assigned to.
            // 4. Fall back to using the Model property on ViewContext.ViewData if none of the above conditions are met.

            if (_hasFor && _hasModel)
            {
                throw new InvalidOperationException(
                    Resources.FormatPartialTagHelper_InvalidModelAttributes(
                        typeof(PartialTagHelper).FullName,
                        ForAttributeName,
                        ModelAttributeName));
            }

            if (_hasModel)
            {
                return Model;
            }

            if (_hasFor)
            {
                return For.Model;
            }

            // A value for Model or For was not specified, fallback to the ViewContext's ViewData model.
            return ViewContext.ViewData.Model;
        }

        private Task RenderPartialViewAsync(TextWriter writer, object model, IViewTemplatingSystem view)
        {
            // Determine which ViewData we should use to construct a new ViewData
            var baseViewData = ViewData ?? ViewContext.ViewData;
            var newViewData = new ViewDataDictionary<object>(baseViewData, model);
            var partialViewContext = new ViewContext(ViewContext, NullView.Instance, newViewData, writer);

            if (For?.Name != null)
            {
                newViewData.TemplateInfo.HtmlFieldPrefix = newViewData.TemplateInfo.GetFullHtmlFieldName(For.Name);
            }

            return view.InvokeAsync(partialViewContext);
        }
    }
}
