// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal class ControllerEndpointFilterInvocationContext : EndpointFilterInvocationContext
{
    public ControllerEndpointFilterInvocationContext(
        ControllerActionDescriptor actionDescriptor,
        ActionContext actionContext,
        ObjectMethodExecutor executor,
        IActionResultTypeMapper mapper,
        object controller,
        object?[]? arguments)
    {
        ActionDescriptor = actionDescriptor;
        ActionContext = actionContext;
        Mapper = mapper;
        Executor = executor;
        Controller = controller;
        Arguments = arguments ?? Array.Empty<object?>();
    }

    public object Controller { get; }

    internal IActionResultTypeMapper Mapper { get; }

    internal ActionContext ActionContext { get; }

    internal ObjectMethodExecutor Executor { get; }

    internal ControllerActionDescriptor ActionDescriptor { get; }

    public override HttpContext HttpContext => ActionContext.HttpContext;

    public override IList<object?> Arguments { get; }

    public override T GetArgument<T>(int index)
    {
        return (T)Arguments[index]!;
    }
}
