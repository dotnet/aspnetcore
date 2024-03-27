// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;

public static class OperationTransformers
{
    public static OpenApiOptions AddHeader(this OpenApiOptions options, string headerName, string defaultValue)
    {
        return options.UseOperationTransformer((operation, context, cancellationToken) =>
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = headerName,
                In = ParameterLocation.Header,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Default = new OpenApiString(defaultValue)
                }
            });
            return Task.CompletedTask;
        });
    }
}
