// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    internal class PageRouteModelFactory
    {
        private static readonly string IndexFileName = "Index" + RazorViewEngine.ViewExtension;
        private readonly RazorPagesOptions _options;
        private readonly ILogger _logger;
        private readonly string _normalizedRootDirectory;
        private readonly string _normalizedAreaRootDirectory;

        public PageRouteModelFactory(
            RazorPagesOptions options,
            ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _normalizedRootDirectory = NormalizeDirectory(options.RootDirectory);
            _normalizedAreaRootDirectory = "/Areas/";
        }

        public PageRouteModel CreateRouteModel(string relativePath, string routeTemplate)
        {
            var viewEnginePath = GetViewEnginePath(_normalizedRootDirectory, relativePath);
            var routeModel = new PageRouteModel(relativePath, viewEnginePath);

            PopulateRouteModel(routeModel, viewEnginePath, routeTemplate);

            return routeModel;
        }

        public PageRouteModel CreateAreaRouteModel(string relativePath, string routeTemplate)
        {
            if (!TryParseAreaPath(relativePath, out var areaResult))
            {
                return null;
            }

            var routeModel = new PageRouteModel(relativePath, areaResult.viewEnginePath, areaResult.areaName);

            var routePrefix = CreateAreaRoute(areaResult.areaName, areaResult.viewEnginePath);
            PopulateRouteModel(routeModel, routePrefix, routeTemplate);
            routeModel.RouteValues["area"] = areaResult.areaName;

            return routeModel;
        }

        private static void PopulateRouteModel(PageRouteModel model, string pageRoute, string routeTemplate)
        {
            model.RouteValues.Add("page", model.ViewEnginePath);

            var selectorModel = CreateSelectorModel(pageRoute, routeTemplate);
            model.Selectors.Add(selectorModel);

            var fileName = Path.GetFileName(model.RelativePath);
            if (!AttributeRouteModel.IsOverridePattern(routeTemplate) &&
                string.Equals(IndexFileName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                // For pages without an override route, and ending in /Index.cshtml, we want to allow
                // incoming routing, but force outgoing routes to match to the path sans /Index.
                selectorModel.AttributeRouteModel.SuppressLinkGeneration = true;

                var index = pageRoute.LastIndexOf('/');
                var parentDirectoryPath = index == -1 ?
                    string.Empty :
                    pageRoute.Substring(0, index);
                model.Selectors.Add(CreateSelectorModel(parentDirectoryPath, routeTemplate));
            }
        }

        // Internal for unit testing
        internal bool TryParseAreaPath(
            string relativePath,
            out (string areaName, string viewEnginePath) result)
        {
            // path = "/Areas/Products/Pages/Manage/Home.cshtml"
            // Result ("Products", "/Manage/Home")
            const string AreaPagesRoot = "/Pages/";

            result = default;
            Debug.Assert(relativePath.StartsWith("/", StringComparison.Ordinal));
            // Parse the area root directory.
            var areaRootEndIndex = relativePath.IndexOf('/', startIndex: 1);
            if (areaRootEndIndex == -1 ||
                areaRootEndIndex >= relativePath.Length - 1 || // There's at least one token after the area root.
                !relativePath.StartsWith(_normalizedAreaRootDirectory, StringComparison.OrdinalIgnoreCase)) // The path must start with area root.
            {
                _logger.UnsupportedAreaPath(relativePath);
                return false;
            }

            // The first directory that follows the area root is the area name.
            var areaEndIndex = relativePath.IndexOf('/', startIndex: areaRootEndIndex + 1);
            if (areaEndIndex == -1 || areaEndIndex == relativePath.Length)
            {
                _logger.UnsupportedAreaPath(relativePath);
                return false;
            }

            var areaName = relativePath.Substring(areaRootEndIndex + 1, areaEndIndex - areaRootEndIndex - 1);
            // Ensure the next token is the "Pages" directory
            if (string.Compare(relativePath, areaEndIndex, AreaPagesRoot, 0, AreaPagesRoot.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                _logger.UnsupportedAreaPath(relativePath);
                return false;
            }

            // Include the trailing slash of the root directory at the start of the viewEnginePath
            var pageNameIndex = areaEndIndex + AreaPagesRoot.Length - 1;
            var viewEnginePath = relativePath.Substring(pageNameIndex, relativePath.Length - pageNameIndex - RazorViewEngine.ViewExtension.Length);

            result = (areaName, viewEnginePath);
            return true;
        }

        private string GetViewEnginePath(string rootDirectory, string path)
        {
            // rootDirectory = "/Pages/AllMyPages/"
            // path = "/Pages/AllMyPages/Home.cshtml"
            // Result = "/Home"
            Debug.Assert(path.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase));
            Debug.Assert(path.EndsWith(RazorViewEngine.ViewExtension, StringComparison.OrdinalIgnoreCase));
            var startIndex = rootDirectory.Length - 1;
            var endIndex = path.Length - RazorViewEngine.ViewExtension.Length;
            return path.Substring(startIndex, endIndex - startIndex);
        }

        private static string CreateAreaRoute(string areaName, string viewEnginePath)
        {
            // AreaName = Products, ViewEnginePath = /List/Categories
            // Result = /Products/List/Categories
            Debug.Assert(!string.IsNullOrEmpty(areaName));
            Debug.Assert(!string.IsNullOrEmpty(viewEnginePath));
            Debug.Assert(viewEnginePath.StartsWith("/", StringComparison.Ordinal));

            var builder = new InplaceStringBuilder(1 + areaName.Length + viewEnginePath.Length);
            builder.Append('/');
            builder.Append(areaName);
            builder.Append(viewEnginePath);

            return builder.ToString();
        }

        private static SelectorModel CreateSelectorModel(string prefix, string routeTemplate)
        {
            return new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel
                {
                    Template = AttributeRouteModel.CombineTemplates(prefix, routeTemplate),
                },
                EndpointMetadata =
                {
                    new PageRouteMetadata(prefix, routeTemplate)
                }
            };
        }

        private static string NormalizeDirectory(string directory)
        {
            Debug.Assert(directory.StartsWith("/", StringComparison.Ordinal));
            if (directory.Length > 1 && !directory.EndsWith("/", StringComparison.Ordinal))
            {
                return directory + "/";
            }

            return directory;
        }
    }
}
