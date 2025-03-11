// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static class OpenApiEndpointRouteBuilderExtensions
{
    /// <summary>
    ///  Helper method to render Swagger UI view for testing.
    /// </summary>
    public static IEndpointConventionBuilder MapSwaggerUi(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/swagger/{documentName}", (string documentName) => Results.Content($$"""
    <html>
    <head>
        <meta charset="UTF-8">
        <title>OpenAPI -- {{documentName}}</title>
        <link rel="stylesheet" type="text/css" href="https://unpkg.com/swagger-ui-dist/swagger-ui.css">
    </head>
    <body>
        <div id="swagger-ui"></div>

        <script src="https://unpkg.com/swagger-ui-dist/swagger-ui-standalone-preset.js"></script>
        <script src="https://unpkg.com/swagger-ui-dist/swagger-ui-bundle.js"></script>

        <script>
            window.onload = function() {
                const ui = SwaggerUIBundle({
                url: "/openapi/{{documentName}}.json",
                    dom_id: '#swagger-ui',
                    deepLinking: true,
                    presets: [
                        SwaggerUIBundle.presets.apis,
                        SwaggerUIStandalonePreset
                    ],
                    plugins: [
                        SwaggerUIBundle.plugins.DownloadUrl
                    ],
                    layout: "StandaloneLayout",
                })
                window.ui = ui
            }
        </script>
    </body>
    </html>
    """, "text/html")).ExcludeFromDescription();
    }

    public static IEndpointRouteBuilder MapTypesWithRef(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/category", (Category category) =>
        {
            return Results.Ok(category);
        });
        endpoints.MapPost("/container", (ContainerType container) =>
        {
            return Results.Ok(container);
        });
        endpoints.MapPost("/root", (Root root) =>
        {
            return Results.Ok(root);
        });
        endpoints.MapPost("/location", (LocationContainer location) =>
        {
            return Results.Ok(location);
        });
        endpoints.MapPost("/parent", (ParentObject parent) =>
        {
            return Results.Ok(parent);
        });
        endpoints.MapPost("/child", (ChildObject child) =>
        {
            return Results.Ok(child);
        });
        return endpoints;
    }

    public sealed class Category
    {
        public required string Name { get; set; }

        public required Category Parent { get; set; }

        public IEnumerable<Tag> Tags { get; set; } = [];
    }

    public sealed class Tag
    {
        public required string Name { get; set; }
    }

    public sealed class ContainerType
    {
        public List<List<string>> Seq1 { get; set; } = [];
        public List<List<string>> Seq2 { get; set; } = [];
    }

    public sealed class Root
    {
        public Item Item1 { get; set; } = null!;
        public Item Item2 { get; set; } = null!;
    }

    public sealed class Item
    {
        public string[] Name { get; set; } = null!;
        public int value { get; set; }
    }

    public sealed class LocationContainer
    {
        public required LocationDto Location { get; set; }
    }

    public sealed class LocationDto
    {
        public required AddressDto Address { get; set; }
    }

    public sealed class AddressDto
    {
        public required LocationDto RelatedLocation { get; set; }
    }

    public sealed class ParentObject
    {
        public int Id { get; set; }
        public List<ChildObject> Children { get; set; } = [];
    }

    public sealed class ChildObject
    {
        public int Id { get; set; }
        public required ParentObject Parent { get; set; }
    }
}
