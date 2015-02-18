// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class TemplateInfo
    {
        private string _htmlFieldPrefix;
        private object _formattedModelValue;

        // Keep a collection of visited objects to prevent infinite recursion.
        private HashSet<object> _visitedObjects;

        public TemplateInfo()
        {
            _htmlFieldPrefix = string.Empty;
            _formattedModelValue = string.Empty;
            _visitedObjects = new HashSet<object>();
        }

        public TemplateInfo(TemplateInfo original)
        {
            FormattedModelValue = original.FormattedModelValue;
            HtmlFieldPrefix = original.HtmlFieldPrefix;
            _visitedObjects = new HashSet<object>(original._visitedObjects);
        }

        /// <summary>
        /// Gets or sets the formatted model value.
        /// </summary>
        /// <remarks>
        /// Will never return <c>null</c> to avoid problems when using HTML helpers within a template.  Otherwise the
        /// helpers could find elements in the `ViewDataDictionary`, not the intended Model properties.
        /// </remarks>
        /// <value>The formatted model value.</value>
        public object FormattedModelValue
        {
            get { return _formattedModelValue; }
            set { _formattedModelValue = value ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets the HTML field prefix.
        /// </summary>
        /// <remarks>
        /// Will never return <c>null</c> for consistency with <see cref="FormattedModelValue"/>.
        /// </remarks>
        /// <value>The HTML field prefix.</value>
        public string HtmlFieldPrefix
        {
            get { return _htmlFieldPrefix; }
            set { _htmlFieldPrefix = value ?? string.Empty; }
        }

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

        public bool Visited(ModelExplorer modelExplorer)
        {
            return _visitedObjects.Contains(modelExplorer.Model ?? modelExplorer.Metadata.ModelType);
        }
    }
}
