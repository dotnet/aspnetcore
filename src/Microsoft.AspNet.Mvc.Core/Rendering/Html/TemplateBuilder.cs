// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Rendering
{
    internal class TemplateBuilder
    {
        private IViewEngine _viewEngine;
        private ViewContext _viewContext;
        private ViewDataDictionary _viewData;
        private ModelMetadata _metadata;
        private string _htmlFieldName;
        private string _templateName;
        private bool _readOnly;
        private object _additionalViewData;

        public TemplateBuilder([NotNull] IViewEngine viewEngine, 
                               [NotNull] ViewContext viewContext, 
                               [NotNull] ViewDataDictionary viewData, 
                               [NotNull] ModelMetadata metadata, 
                               string htmlFieldName, 
                               string templateName, 
                               bool readOnly, 
                               object additionalViewData)
        {
            _viewEngine = viewEngine;
            _viewContext = viewContext;
            _viewData = viewData;
            _metadata = metadata;
            _htmlFieldName = htmlFieldName;
            _templateName = templateName;
            _readOnly = readOnly;
            _additionalViewData = additionalViewData;
        }

        public string Build()
        {
            if (_metadata.ConvertEmptyStringToNull && string.Empty.Equals(_metadata.Model))
            {
                _metadata.Model = null;
            }

            var formattedModelValue = _metadata.Model;
            if (_metadata.Model == null && _readOnly)
            {
                formattedModelValue = _metadata.NullDisplayText;
            }

            var formatString = _readOnly ? _metadata.DisplayFormatString : _metadata.EditFormatString;

            if (_metadata.Model != null && !string.IsNullOrEmpty(formatString))
            {
                formattedModelValue = string.Format(CultureInfo.CurrentCulture, formatString, _metadata.Model);
            }

            // Normally this shouldn't happen, unless someone writes their own custom Object templates which
            // don't check to make sure that the object hasn't already been displayed
            if (_viewData.TemplateInfo.Visited(_metadata))
            {
                return string.Empty;
            }

            var viewData = new ViewDataDictionary(_viewData)
            {
                Model = _metadata.Model,
                ModelMetadata = _metadata
            };

            viewData.TemplateInfo.FormattedModelValue = formattedModelValue;
            viewData.TemplateInfo.HtmlFieldPrefix = _viewData.TemplateInfo.GetFullHtmlFieldName(_htmlFieldName);

            if (_additionalViewData != null)
            {
                foreach (KeyValuePair<string, object> kvp in HtmlHelper.ObjectToDictionary(_additionalViewData))
                {
                    viewData[kvp.Key] = kvp.Value;
                }
            }

            object visitedObjectsKey = _metadata.Model ?? _metadata.RealModelType;
            viewData.TemplateInfo.AddVisited(visitedObjectsKey);

            var templateRenderer = new TemplateRenderer(_viewEngine, _viewContext, viewData, _templateName, _readOnly);

            return templateRenderer.Render();
        }
    }
}
