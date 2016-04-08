// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace HtmlGenerationWebSite.Components
{
    public class CheckViewData : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var metadata = ViewData.ModelMetadata;
            var writer = ViewContext.Writer;
            writer.WriteLine("<h5>Check View Data view component</h5>");
            writer.WriteLine($"<div class=\"col-md-3\">MetadataKind: '{ metadata.MetadataKind }'</div>");
            writer.WriteLine($"<div class=\"col-md-3\">ModelType: '{ metadata.ModelType.Name }'</div>");
            if (metadata.MetadataKind == ModelMetadataKind.Property)
            {
                writer.WriteLine($"<div class=\"col-md-3\">PropertyName: '{ metadata.PropertyName }'</div>");
            }

            return View();
        }
    }
}
