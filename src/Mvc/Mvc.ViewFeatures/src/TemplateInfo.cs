// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class TemplateInfo
    {
        // Keep a collection of visited objects to prevent infinite recursion.
        private readonly HashSet<object> _visitedObjects;

        private object _formattedModelValue;
        private string _htmlFieldPrefix;

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
        /// <value>The formatted model value.</value>
        /// <remarks>
        /// Will never return <c>null</c> to avoid problems when using HTML helpers within a template.  Otherwise the
        /// helpers could find elements in the `ViewDataDictionary`, not the intended Model properties.
        /// </remarks>
        public object FormattedModelValue
        {
            get { return _formattedModelValue; }
            set { _formattedModelValue = value ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets the HTML field prefix.
        /// </summary>
        /// <value>The HTML field prefix.</value>
        /// <remarks>
        /// Will never return <c>null</c> for consistency with <see cref="FormattedModelValue"/>.
        /// </remarks>
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

        /// <summary>
        /// Returns the full HTML element name for the specified <paramref name="partialFieldName"/>.
        /// </summary>
        /// <param name="partialFieldName">Expression name, relative to the current model.</param>
        /// <returns>Fully-qualified expression name for <paramref name="partialFieldName"/>.</returns>
        public string GetFullHtmlFieldName(string partialFieldName)
        {
            if (string.IsNullOrEmpty(partialFieldName))
            {
                return HtmlFieldPrefix;
            }

            if (string.IsNullOrEmpty(HtmlFieldPrefix))
            {
                return partialFieldName;
            }

            if (partialFieldName.StartsWith("[", StringComparison.Ordinal))
            {
                // The partialFieldName might represent an indexer access, in which case combining
                // with a 'dot' would be invalid.
                return HtmlFieldPrefix + partialFieldName;
            }

            return HtmlFieldPrefix + "." + partialFieldName;
        }

        public bool Visited(ModelExplorer modelExplorer)
        {
            return _visitedObjects.Contains(modelExplorer.Model ?? modelExplorer.Metadata.ModelType);
        }
    }
}
