// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Application model component for RazorPages.
    /// </summary>
    public class PageApplicationModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PageApplicationModel"/>.
        /// </summary>
        /// <param name="relativePath">The application relative path of the page.</param>
        /// <param name="viewEnginePath">The path relative to the base path for page discovery.</param>
        public PageApplicationModel(string relativePath, string viewEnginePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (viewEnginePath == null)
            {
                throw new ArgumentNullException(nameof(viewEnginePath));
            }

            RelativePath = relativePath;
            ViewEnginePath = viewEnginePath;

            Filters = new List<IFilterMetadata>();
            Properties = new Dictionary<object, object>();
            Selectors = new List<SelectorModel>();
        }

        /// <summary>
        /// A copy constructor for <see cref="PageApplicationModel"/>.
        /// </summary>
        /// <param name="other">The <see cref="PageApplicationModel"/> to copy from.</param>
        public PageApplicationModel(PageApplicationModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            RelativePath = other.RelativePath;
            ViewEnginePath = other.ViewEnginePath;

            Filters = new List<IFilterMetadata>(other.Filters);
            Properties = new Dictionary<object, object>(other.Properties);

            Selectors = new List<SelectorModel>(other.Selectors.Select(m => new SelectorModel(m)));
        }

        /// <summary>
        /// Gets the application root relative path for the page.
        /// </summary>
        public string RelativePath { get; }

        /// <summary>
        /// Gets the path relative to the base path for page discovery.
        /// </summary>
        public string ViewEnginePath { get; }

        /// <summary>
        /// Gets the applicable <see cref="IFilterMetadata"/> instances.
        /// </summary>
        public IList<IFilterMetadata> Filters { get; }

        /// <summary>
        /// Stores arbitrary metadata properties associated with the <see cref="PageApplicationModel"/>.
        /// </summary>
        public IDictionary<object, object> Properties { get; }

        /// <summary>
        /// Gets the <see cref="SelectorModel"/> instances.
        /// </summary>
        public IList<SelectorModel> Selectors { get; }
    }
}