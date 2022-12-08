// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// TODO
/// </summary>
public class RazorComponentResult : IResult
{
    private readonly Type? _componentType;
    private readonly IReadOnlyDictionary<string, object?>? _parameters;
    private Dictionary<string, object?>? _modifiedParameters;
    private readonly IComponent? _componentInstance;

    /// <summary>
    /// TODO
    /// </summary>
    public ComponentRenderMode RenderMode { get; set; } = ComponentRenderMode.Unspecified;

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
    public RazorComponentResult(Type componentType, IReadOnlyDictionary<string, object?>? parameters)
        : this(componentType)
    {
        _parameters = parameters;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public RazorComponentResult(IComponent component)
    {
        _componentInstance = component;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public RazorComponentResult(IComponent component, IReadOnlyDictionary<string, object?>? parameters)
        : this(component)
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
        var parameters = _modifiedParameters ?? _parameters;
        return _componentInstance is not null
            ? renderer.HandleRequest(httpContext, RenderMode, _componentInstance, parameters)
            : renderer.HandleRequest(httpContext, RenderMode, _componentType!, parameters);
    }
}

/// <summary>
/// TODO
/// </summary>
public class RazorComponentResult<TComponent> : RazorComponentResult where TComponent : IComponent
{
    /// <summary>
    /// TODO
    /// </summary>
    public RazorComponentResult() : base(typeof(TComponent))
    {
    }

    /// <summary>
    /// TODO
    /// </summary>
    public RazorComponentResult(IReadOnlyDictionary<string, object?>? parameters) : base(typeof(TComponent), parameters)
    {
    }

    /// <summary>
    /// TODO
    /// </summary>
    public RazorComponentResult(TComponent component, IReadOnlyDictionary<string, object?>? parameters = null)
        : base(component, parameters)
    {
    }
}
