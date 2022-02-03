// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using HtmlGenerationWebSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace HtmlGenerationWebSite.Components;

public class CheckViewData___LackModel : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var metadata = ViewData.ModelMetadata;
        var writer = ViewContext.Writer;
        writer.WriteLine("<h5>Check View Data - LackModel view component</h5>");
        writer.WriteLine($"<div class=\"col-md-3\">MetadataKind: '{ metadata.MetadataKind }'</div>");
        writer.WriteLine($"<div class=\"col-md-3\">ModelType: '{ metadata.ModelType.Name }'</div>");
        if (metadata.MetadataKind == ModelMetadataKind.Property)
        {
            writer.WriteLine($"<div class=\"col-md-3\">PropertyName: '{ metadata.PropertyName }'</div>");
        }

        // Confirm view component is able to set the model to anything.
        ViewData.Model = 78.9;

        // Expected metadata is for typeof(object).
        metadata = ViewData.ModelMetadata;
        writer.WriteLine("<h5>Check View Data - LackModel view component after setting Model to 78.9</h5>");
        writer.WriteLine($"<div class=\"col-md-3\">MetadataKind: '{ metadata.MetadataKind }'</div>");
        writer.WriteLine($"<div class=\"col-md-3\">ModelType: '{ metadata.ModelType.Name }'</div>");
        if (metadata.MetadataKind == ModelMetadataKind.Property)
        {
            writer.WriteLine($"<div class=\"col-md-3\">PropertyName: '{ metadata.PropertyName }'</div>");
        }

        TemplateModel templateModel = new SuperTemplateModel();

        return View(templateModel);
    }
}
