// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.ViewEngines
{
    /// <summary>
    /// Represents the result of a view engine.
    /// </summary>
    public class ViewEngineResult
    {
        private ViewEngineResult()
        {
        }

        /// <summary>
        /// The list of locations searched.
        /// </summary>
        public IEnumerable<string> SearchedLocations { get; private set; }

        /// <summary>
        /// The <see cref="IView"/>.
        /// </summary>
        public IView View { get; private set; }

        /// <summary>
        /// Gets or sets the name of the view.
        /// </summary>
        public string ViewName { get; private set; }

        /// <summary>
        /// Whether the result was successful
        /// </summary>
        public bool Success => View != null;

        /// <summary>
        /// Returns a result that represents when a view is not found.
        /// </summary>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="searchedLocations">The locations searched.</param>
        /// <returns>The not found result.</returns>
        public static ViewEngineResult NotFound(
            string viewName,
            IEnumerable<string> searchedLocations)
        {
            if (viewName == null)
            {
                throw new ArgumentNullException(nameof(viewName));
            }

            if (searchedLocations == null)
            {
                throw new ArgumentNullException(nameof(searchedLocations));
            }

            return new ViewEngineResult
            {
                SearchedLocations = searchedLocations,
                ViewName = viewName,
            };
        }

        /// <summary>
        /// Returns a result when a view is found.
        /// </summary>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="view">The <see cref="IView"/>.</param>
        /// <returns>The found result.</returns>
        public static ViewEngineResult Found(string viewName, IView view)
        {
            if (viewName == null)
            {
                throw new ArgumentNullException(nameof(viewName));
            }

            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            return new ViewEngineResult
            {
                View = view,
                ViewName = viewName,
            };
        }

        /// <summary>
        /// Ensure this <see cref="ViewEngineResult"/> was successful.
        /// </summary>
        /// <param name="originalLocations">
        /// Additional <see cref="SearchedLocations"/> to include in the thrown <see cref="InvalidOperationException"/>
        /// if <see cref="Success"/> is <c>false</c>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="Success"/> is <c>false</c>.
        /// </exception>
        /// <returns>This <see cref="ViewEngineResult"/> if <see cref="Success"/> is <c>true</c>.</returns>
        public ViewEngineResult EnsureSuccessful(IEnumerable<string> originalLocations)
        {
            if (!Success)
            {
                var locations = string.Empty;
                if (originalLocations != null && originalLocations.Any())
                {
                    locations = Environment.NewLine + string.Join(Environment.NewLine, originalLocations);
                }

                if (SearchedLocations.Any())
                {
                    locations += Environment.NewLine + string.Join(Environment.NewLine, SearchedLocations);
                }

                throw new InvalidOperationException(Resources.FormatViewEngine_ViewNotFound(ViewName, locations));
            }

            return this;
        }
    }
}
