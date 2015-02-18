// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    internal class TemplateBuilder
    {
        private IViewEngine _viewEngine;
        private ViewContext _viewContext;
        private ViewDataDictionary _viewData;
        private ModelExplorer _modelExplorer;
        private object _model;
        private ModelMetadata _metadata;
        private string _htmlFieldName;
        private string _templateName;
        private bool _readOnly;
        private object _additionalViewData;

        public TemplateBuilder([NotNull] IViewEngine viewEngine,
                               [NotNull] ViewContext viewContext,
                               [NotNull] ViewDataDictionary viewData,
                               [NotNull] ModelExplorer modelExplorer,
                               string htmlFieldName,
                               string templateName,
                               bool readOnly,
                               object additionalViewData)
        {
            _viewEngine = viewEngine;
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

        public string Build()
        {
            if (_metadata.ConvertEmptyStringToNull && string.Empty.Equals(_model))
            {
                _model = null;
            }

            var formattedModelValue = _model;
            if (_model == null && _readOnly)
            {
                formattedModelValue = _metadata.NullDisplayText;
            }

            var formatString = _readOnly ? _metadata.DisplayFormatString : _metadata.EditFormatString;

            if (_model != null && !string.IsNullOrEmpty(formatString))
            {
                formattedModelValue = string.Format(CultureInfo.CurrentCulture, formatString, _model);
            }

            // Normally this shouldn't happen, unless someone writes their own custom Object templates which
            // don't check to make sure that the object hasn't already been displayed
            if (_viewData.TemplateInfo.Visited(_modelExplorer))
            {
                return string.Empty;
            }

            // We need to copy the ModelExplorer to copy the model metadata. Otherwise we might
            // lose track of the model type/property. Passing null here explicitly, because
            // this might be a typed VDD, and the model value might not be compatible.
            var viewData = new ViewDataDictionary(_viewData, model: null);

            // We're setting ModelExplorer in order to preserve the model metadata of the original
            // _viewData even though _model may be null.
            viewData.ModelExplorer = _modelExplorer.GetExplorerForModel(_model);

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

            var templateRenderer = new TemplateRenderer(_viewEngine, _viewContext, viewData, _templateName, _readOnly);

            return templateRenderer.Render();
        }
    }
}
