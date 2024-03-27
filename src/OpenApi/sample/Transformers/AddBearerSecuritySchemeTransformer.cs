// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = (authenticationSchemeProvider is not null)
                ? await authenticationSchemeProvider.GetAllSchemesAsync()
                : [];
        var requirements = authenticationSchemes
                .Where(authScheme => authScheme.Name == "Bearer")
                .ToDictionary(
                    (authScheme) => authScheme.Name,
                    (authScheme) => new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer", // "bearer" refers to the header name here
                        In = ParameterLocation.Header,
                        BearerFormat = "Json Web Token"
                    });
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = requirements;
    }
}
