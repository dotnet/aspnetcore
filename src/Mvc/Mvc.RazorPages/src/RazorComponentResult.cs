// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// TODO
/// </summary>
public class RazorComponentResult : IResult
{
    private readonly Type _componentType;
    private readonly IReadOnlyDictionary<string, object?>? _parameters;
    private Dictionary<string, object?>? _modifiedParameters;

    /// <summary>
    /// TODO
    /// </summary>
    public RazorComponentResult(Type componentType)
    {
        _componentType = componentType;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public RazorComponentResult(Type componentType, IReadOnlyDictionary<string, object?>? parameters) : this(componentType)
    {
        _parameters = parameters;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public RazorComponentResult WithParameter(string name, object? value)
    {
        if (_modifiedParameters is null)
        {
            _modifiedParameters = _parameters is null ? new() : new(_parameters);
        }

        _modifiedParameters[name] = value;

        return this;
    }

    Task IResult.ExecuteAsync(HttpContext httpContext)
    {
        var renderer = httpContext.RequestServices.GetRequiredService<PassiveComponentRenderer>();
        return renderer.HandleRequest(httpContext, _componentType, _modifiedParameters ?? _parameters);
    }
}
