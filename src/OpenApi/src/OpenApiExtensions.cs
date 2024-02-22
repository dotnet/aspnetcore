// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;

internal static class OpenApiExtensions
{
    public static OperationType ToOperationType(this string? method) => method switch
    {
        "GET" => OperationType.Get,
        "POST" => OperationType.Post,
        "PUT" => OperationType.Put,
        "DELETE" => OperationType.Delete,
        "OPTIONS" => OperationType.Options,
        "HEAD" => OperationType.Head,
        "PATCH" => OperationType.Patch,
        "TRACE" => OperationType.Trace,
        _ => OperationType.Get
    };

    public static ParameterLocation ToParameterLocation(this BindingSource source)
    {
        if (source == BindingSource.Query)
        {
            return ParameterLocation.Query;
        }
        if (source == BindingSource.Path)
        {
            return ParameterLocation.Path;
        }
        if (source == BindingSource.Header)
        {
            return ParameterLocation.Header;
        }
        return ParameterLocation.Query;
    }
}
