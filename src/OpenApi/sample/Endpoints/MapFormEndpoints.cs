// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

public static class FormEndpoints
{
    public static IEndpointRouteBuilder MapFormEndpoints(this WebApplication app)
    {
        var forms = app.MapGroup("/forms")
            .WithGroupName("forms");

        if (app.Environment.IsDevelopment())
        {
            forms.DisableAntiforgery();
        }

        forms.MapPost("/form-file", (IFormFile resume) => Results.Ok(resume.FileName));
        forms.MapPost("/form-files", (IFormFileCollection files) => Results.Ok(files.Count));
        forms.MapPost("/form-file-multiple", (IFormFile resume, IFormFileCollection files) => Results.Ok(files.Count + resume.FileName));
        // Disable warnings because RDG does not support complex form binding yet.
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning disable RDG003 // Unable to resolve parameter
        forms.MapPost("/form-todo", ([FromForm] Todo todo) => Results.Ok(todo));
        forms.MapPost("/forms-pocos-and-files", ([FromForm] Todo todo, IFormFile file) => Results.Ok(new { Todo = todo, File = file.FileName }));
#pragma warning restore RDG003 // Unable to resolve parameter
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

        return app;
    }
}
