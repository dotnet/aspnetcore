// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public readonly struct LocateViewResult
    {
        public LocateViewResult(string viewName, IViewTemplatingSystem viewTemplate)
        {
            ViewName = viewName;
            ViewTemplate = viewTemplate ?? throw new ArgumentNullException(nameof(viewTemplate));
            SearchedLocations = null;
        }

        public LocateViewResult(string viewName, IEnumerable<string> searchedLocations)
        {
            ViewName = viewName;
            ViewTemplate = null;
            SearchedLocations = searchedLocations ?? throw new ArgumentNullException(nameof(searchedLocations));
        }

        public string ViewName { get; }

        public IViewTemplatingSystem ViewTemplate { get; }

        public IEnumerable<string> SearchedLocations { get; }

        public bool Success => ViewTemplate != null;

        public void EnsureSuccessful()
        {
            if (Success)
            {
                return;
            }

            var locations = Environment.NewLine + string.Join(Environment.NewLine, SearchedLocations);
            throw new InvalidOperationException(Resources.FormatViewEngine_ViewNotFound(ViewName, locations));
        }
    }
}
