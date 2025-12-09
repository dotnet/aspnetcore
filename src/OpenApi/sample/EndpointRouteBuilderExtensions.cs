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
}
