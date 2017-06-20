// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class PageSelectorModel
    {
        private const string IndexFileName = "Index.cshtml";

        public static void PopulateDefaults(PageRouteModel model, string routeTemplate)
        {
            if (AttributeRouteModel.IsOverridePattern(routeTemplate))
            {
                throw new InvalidOperationException(string.Format(
                    Resources.PageActionDescriptorProvider_RouteTemplateCannotBeOverrideable,
                    model.RelativePath));
            }

            var selectorModel = CreateSelectorModel(model.ViewEnginePath, routeTemplate);
            model.Selectors.Add(selectorModel);

            var fileName = Path.GetFileName(model.RelativePath);
            if (string.Equals(IndexFileName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                // For pages ending in /Index.cshtml, we want to allow incoming routing, but
                // force outgoing routes to match to the path sans /Index.
                selectorModel.AttributeRouteModel.SuppressLinkGeneration = true;

                var parentDirectoryPath = model.ViewEnginePath;
                var index = parentDirectoryPath.LastIndexOf('/');
                if (index == -1)
                {
                    parentDirectoryPath = string.Empty;
                }
                else
                {
                    parentDirectoryPath = parentDirectoryPath.Substring(0, index);
                }
                model.Selectors.Add(CreateSelectorModel(parentDirectoryPath, routeTemplate));
            }
        }

        private static SelectorModel CreateSelectorModel(string prefix, string template)
        {
            return new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel
                {
                    Template = AttributeRouteModel.CombineTemplates(prefix, template),
                }
            };
        }
    }
}
