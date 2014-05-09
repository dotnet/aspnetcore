// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewEngine : IViewEngine
    {
        private const string ViewExtension = ".cshtml";

        private static readonly string[] _viewLocationFormats =
        {
            "/Views/{1}/{0}" + ViewExtension,
            "/Views/Shared/{0}" + ViewExtension,
        };

        private static readonly string[] _areaViewLocationFormats =
        {
            "/Areas/{2}/Views/{1}/{0}" + ViewExtension,
            "/Areas/{2}/Views/Shared/{0}" + ViewExtension,
            "/Views/Shared/{0}" + ViewExtension,
        };

        private readonly IVirtualPathViewFactory _virtualPathFactory;

        public RazorViewEngine(IVirtualPathViewFactory virtualPathFactory)
        {
            _virtualPathFactory = virtualPathFactory;
        }

        public IEnumerable<string> ViewLocationFormats
        {
            get { return _viewLocationFormats; }
        }

        public ViewEngineResult FindView([NotNull] IDictionary<string, object> context,
                                         [NotNull] string viewName)
        {
            var viewEngineResult = CreateViewEngineResult(context, viewName);
            return viewEngineResult;
        }

        public ViewEngineResult FindPartialView([NotNull] IDictionary<string, object> context,
                                                [NotNull] string partialViewName)
        {
            return FindView(context, partialViewName);
        }

        private ViewEngineResult CreateViewEngineResult([NotNull] IDictionary<string, object> context,
                                                        [NotNull] string viewName)
        {
            var nameRepresentsPath = IsSpecificPath(viewName);

            if (nameRepresentsPath)
            {
                if (!viewName.EndsWith(ViewExtension, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        Resources.FormatViewMustEndInExtension(viewName, ViewExtension));
                }

                var view = _virtualPathFactory.CreateInstance(viewName);
                return view != null ? ViewEngineResult.Found(viewName, view) :
                                      ViewEngineResult.NotFound(viewName, new[] { viewName });
            }
            else
            {
                var controllerName = context.GetValueOrDefault<string>("controller");
                var areaName = context.GetValueOrDefault<string>("area");
                var potentialPaths = GetViewSearchPaths(viewName, controllerName, areaName);

                foreach (var path in potentialPaths)
                {
                    var view = _virtualPathFactory.CreateInstance(path);
                    if (view != null)
                    {
                        return ViewEngineResult.Found(viewName, view);
                    }
                }

                return ViewEngineResult.NotFound(viewName, potentialPaths);
            }
        }

        private static bool IsSpecificPath(string name)
        {
            char c = name[0];
            return name[0] == '~' || name[0] == '/';
        }

        private IEnumerable<string> GetViewSearchPaths(string viewName, string controllerName, string areaName)
        {
            IEnumerable<string> unformattedPaths;

            if (string.IsNullOrEmpty(areaName))
            {
                // If no areas then no need to search area locations.
                unformattedPaths = _viewLocationFormats;
            }
            else
            {
                // If there's an area provided only search area view locations
                unformattedPaths = _areaViewLocationFormats;
            }

            var formattedPaths = unformattedPaths.Select(path =>
                string.Format(CultureInfo.InvariantCulture, path, viewName, controllerName, areaName)
            );

            return formattedPaths;
        }
    }
}
