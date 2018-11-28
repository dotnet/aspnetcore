// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class TemplateBuilder
    {
        private readonly IViewEngine _viewEngine;
        private readonly IViewBufferScope _bufferScope;
        private readonly ViewContext _viewContext;
        private readonly ViewDataDictionary _viewData;
        private readonly ModelExplorer _modelExplorer;
        private object _model;
        private readonly ModelMetadata _metadata;
        private readonly string _htmlFieldName;
        private readonly string _templateName;
        private readonly bool _readOnly;
        private readonly object _additionalViewData;

        public TemplateBuilder(
            IViewEngine viewEngine,
            IViewBufferScope bufferScope,
            ViewContext viewContext,
            ViewDataDictionary viewData,
            ModelExplorer modelExplorer,
            string htmlFieldName,
            string templateName,
            bool readOnly,
            object additionalViewData)
        {
            if (viewEngine == null)
            {
                throw new ArgumentNullException(nameof(viewEngine));
            }

            if (bufferScope == null)
            {
                throw new ArgumentNullException(nameof(bufferScope));
            }

            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            if (viewData == null)
            {
                throw new ArgumentNullException(nameof(viewData));
            }

            if (modelExplorer == null)
            {
                throw new ArgumentNullException(nameof(modelExplorer));
            }

            _viewEngine = viewEngine;
            _bufferScope = bufferScope;
            _viewContext = viewContext;
            _viewData = viewData;
            _modelExplorer = modelExplorer;
            _htmlFieldName = htmlFieldName;
            _templateName = templateName;
            _readOnly = readOnly;
            _additionalViewData = additionalViewData;

            _model = modelExplorer.Model;
            _metadata = modelExplorer.Metadata;
        }

        public IHtmlContent Build()
        {
            if (_metadata.ConvertEmptyStringToNull && string.Empty.Equals(_model))
            {
                _model = null;
            }

            // Normally this shouldn't happen, unless someone writes their own custom Object templates which
            // don't check to make sure that the object hasn't already been displayed
            if (_viewData.TemplateInfo.Visited(_modelExplorer))
            {
                return HtmlString.Empty;
            }

            // Create VDD of type object so any model type is allowed.
            var viewData = new ViewDataDictionary<object>(_viewData);

            // Create a new ModelExplorer in order to preserve the model metadata of the original _viewData even
            // though _model may have been reset to null. Otherwise we might lose track of the model type /property.
            viewData.ModelExplorer = _modelExplorer.GetExplorerForModel(_model);

            var formatString = _readOnly ? 
                viewData.ModelMetadata.DisplayFormatString : 
                viewData.ModelMetadata.EditFormatString;

            var formattedModelValue = _model;
            if (_model == null)
            {
                if (_readOnly)
                {
                    formattedModelValue = _metadata.NullDisplayText;
                }
            }
            else if (!string.IsNullOrEmpty(formatString))
            {
                formattedModelValue = string.Format(CultureInfo.CurrentCulture, formatString, _model);
            }
            else if ((string.Equals("week", _templateName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("week", viewData.ModelMetadata.DataTypeName, StringComparison.OrdinalIgnoreCase)))
            {
                // "week" is a new HTML5 input type that only will be rendered in Rfc3339 mode
                formattedModelValue = FormatWeekHelper.GetFormattedWeek(_modelExplorer);
            }
            else if (viewData.ModelMetadata.IsEnum && _model is Enum modelEnum)
            {
                // Cover the case where the model is an enum and we want the string value of it
                var value = modelEnum.ToString("d");
                var enumGrouped = viewData.ModelMetadata.EnumGroupedDisplayNamesAndValues;
                Debug.Assert(enumGrouped != null);
                foreach (var kvp in enumGrouped)
                {
                    if (kvp.Value == value)
                    {
                        // Creates a ModelExplorer with the same Metadata except that the Model is a string instead of an Enum
                        formattedModelValue = kvp.Key.Name;
                        break;
                    }
                }
            }

            viewData.TemplateInfo.FormattedModelValue = formattedModelValue;
            viewData.TemplateInfo.HtmlFieldPrefix = _viewData.TemplateInfo.GetFullHtmlFieldName(_htmlFieldName);

            if (_additionalViewData != null)
            {
                foreach (var kvp in HtmlHelper.ObjectToDictionary(_additionalViewData))
                {
                    viewData[kvp.Key] = kvp.Value;
                }
            }

            var visitedObjectsKey = _model ?? _modelExplorer.ModelType;
            viewData.TemplateInfo.AddVisited(visitedObjectsKey);

            var templateRenderer = new TemplateRenderer(
                _viewEngine,
                _bufferScope,
                _viewContext,
                viewData,
                _templateName,
                _readOnly);

            return templateRenderer.Render();
        }
    }
}
