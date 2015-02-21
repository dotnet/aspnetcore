// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewEngineResult
    {
        private ViewEngineResult()
        {
        }

        public IEnumerable<string> SearchedLocations { get; private set; }

        public IView View { get; private set; }

        public string ViewName { get; private set; }

        public bool Success
        {
            get { return View != null; }
        }

        public static ViewEngineResult NotFound([NotNull] string viewName,
                                                [NotNull] IEnumerable<string> searchedLocations)
        {
            return new ViewEngineResult
            {
                SearchedLocations = searchedLocations,
                ViewName = viewName,
            };
        }

        public static ViewEngineResult Found([NotNull] string viewName, [NotNull] IView view)
        {
            return new ViewEngineResult
            {
                View = view,
                ViewName = viewName,
            };
        }

        public ViewEngineResult EnsureSuccessful()
        {
            if (!Success)
            {
                var locations = string.Empty;
                if (SearchedLocations != null)
                {
                    locations = Environment.NewLine + string.Join(Environment.NewLine, SearchedLocations);
                }

                throw new InvalidOperationException(Resources.FormatViewEngine_ViewNotFound(ViewName, locations));
            }

            return this;
        }
    }
}
