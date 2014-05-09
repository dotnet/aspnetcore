// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class TemplateInfo
    {
        // Keep a collection of visited objects to prevent infinite recursion.
        private HashSet<object> _visitedObjects;

        public TemplateInfo()
        {
            FormattedModelValue = string.Empty;
            HtmlFieldPrefix = string.Empty;
            _visitedObjects = new HashSet<object>();
        }

        public TemplateInfo(TemplateInfo original)
        {
            FormattedModelValue = original.FormattedModelValue;
            HtmlFieldPrefix = original.HtmlFieldPrefix;
            _visitedObjects = new HashSet<object>(original._visitedObjects);
        }

        public object FormattedModelValue { get; set; }

        public string HtmlFieldPrefix { get; set; }

        public int TemplateDepth
        {
            get { return _visitedObjects.Count; }
        }

        public bool AddVisited(object value)
        {
            return _visitedObjects.Add(value);
        }

        public string GetFullHtmlFieldName(string partialFieldName)
        {
            if (string.IsNullOrEmpty(partialFieldName))
            {
                return HtmlFieldPrefix;
            }
            else if (string.IsNullOrEmpty(HtmlFieldPrefix))
            {
                return partialFieldName;
            }
            else if (partialFieldName.StartsWith("[", StringComparison.Ordinal))
            {
                // The partialFieldName might represent an indexer access, in which case combining
                // with a 'dot' would be invalid.
                return HtmlFieldPrefix + partialFieldName;
            }
            else
            {
                return HtmlFieldPrefix + "." + partialFieldName;
            }
        }

        public bool Visited(ModelMetadata metadata)
        {
            return _visitedObjects.Contains(metadata.Model ?? metadata.ModelType);
        }
    }
}
