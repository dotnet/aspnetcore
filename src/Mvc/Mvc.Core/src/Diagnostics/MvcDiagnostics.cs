// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Diagnostics;

/// <summary>
/// An <see cref="EventData"/> that occurs before an action.
/// </summary>
public sealed class BeforeActionEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeAction";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeActionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="routeData">The <see cref="RouteData"/>.</param>
    public BeforeActionEventData(ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData)
    {
        ActionDescriptor = actionDescriptor;
        HttpContext = httpContext;
        RouteData = routeData;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// The route data.
    /// </summary>
    public RouteData RouteData { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
        2 => new KeyValuePair<string, object>(nameof(RouteData), RouteData),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after an action.
/// </summary>
public sealed class AfterActionEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterAction";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterActionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="routeData">The <see cref="RouteData"/>.</param>
    public AfterActionEventData(ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData)
    {
        ActionDescriptor = actionDescriptor;
        HttpContext = httpContext;
        RouteData = routeData;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// The route data.
    /// </summary>
    public RouteData RouteData { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
        2 => new KeyValuePair<string, object>(nameof(RouteData), RouteData),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before <see cref="IAuthorizationFilter.OnAuthorization(AuthorizationFilterContext)"/>.
/// </summary>
public sealed class BeforeAuthorizationFilterOnAuthorizationEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnAuthorization";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeAuthorizationFilterOnAuthorizationEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="authorizationContext">The <see cref="AuthorizationFilterContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public BeforeAuthorizationFilterOnAuthorizationEventData(ActionDescriptor actionDescriptor, AuthorizationFilterContext authorizationContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        AuthorizationContext = authorizationContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The authorization context.
    /// </summary>
    public AuthorizationFilterContext AuthorizationContext { get; }

    /// <summary>
    /// The authorization filter.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(AuthorizationContext), AuthorizationContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after <see cref="IAuthorizationFilter.OnAuthorization(AuthorizationFilterContext)"/>.
/// </summary>
public sealed class AfterAuthorizationFilterOnAuthorizationEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnAuthorization";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterAuthorizationFilterOnAuthorizationEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="authorizationContext">The <see cref="AuthorizationFilterContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public AfterAuthorizationFilterOnAuthorizationEventData(ActionDescriptor actionDescriptor, AuthorizationFilterContext authorizationContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        AuthorizationContext = authorizationContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The authorization context.
    /// </summary>
    public AuthorizationFilterContext AuthorizationContext { get; }

    /// <summary>
    /// The authorization filter.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(AuthorizationContext), AuthorizationContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before <see cref="IResourceFilter"/> execution.
/// </summary>
public sealed class BeforeResourceFilterOnResourceExecutionEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnResourceExecution";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeResourceFilterOnResourceExecutionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resourceExecutingContext">The <see cref="ResourceExecutingContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public BeforeResourceFilterOnResourceExecutionEventData(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResourceExecutingContext = resourceExecutingContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ResourceExecutingContext ResourceExecutingContext { get; }

    /// <summary>
    /// The resource filter that will run.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResourceExecutingContext), ResourceExecutingContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after <see cref="IResourceFilter"/> execution.
/// </summary>
public sealed class AfterResourceFilterOnResourceExecutionEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnResourceExecution";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterResourceFilterOnResourceExecutionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resourceExecutedContext">The <see cref="ResourceExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public AfterResourceFilterOnResourceExecutionEventData(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResourceExecutedContext = resourceExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ResourceExecutedContext ResourceExecutedContext { get; }

    /// <summary>
    /// The resource filter that will be run.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResourceExecutedContext), ResourceExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before <see cref="IResourceFilter.OnResourceExecuting(ResourceExecutingContext)"/>.
/// </summary>
public sealed class BeforeResourceFilterOnResourceExecutingEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnResourceExecuting";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeResourceFilterOnResourceExecutingEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resourceExecutingContext">The <see cref="ResourceExecutingContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public BeforeResourceFilterOnResourceExecutingEventData(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResourceExecutingContext = resourceExecutingContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ResourceExecutingContext ResourceExecutingContext { get; }

    /// <summary>
    /// The resource filter that will run.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResourceExecutingContext), ResourceExecutingContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after <see cref="IResourceFilter.OnResourceExecuting(ResourceExecutingContext)"/>.
/// </summary>
public sealed class AfterResourceFilterOnResourceExecutingEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnResourceExecuting";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterResourceFilterOnResourceExecutingEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resourceExecutingContext">The <see cref="ResourceExecutingContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public AfterResourceFilterOnResourceExecutingEventData(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResourceExecutingContext = resourceExecutingContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ResourceExecutingContext ResourceExecutingContext { get; }

    /// <summary>
    /// The resource filter that ran.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResourceExecutingContext), ResourceExecutingContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before <see cref="IResourceFilter.OnResourceExecuted(ResourceExecutedContext)"/>.
/// </summary>
public sealed class BeforeResourceFilterOnResourceExecutedEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnResourceExecuted";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeResourceFilterOnResourceExecutedEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resourceExecutedContext">The <see cref="ResourceExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public BeforeResourceFilterOnResourceExecutedEventData(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResourceExecutedContext = resourceExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ResourceExecutedContext ResourceExecutedContext { get; }

    /// <summary>
    /// The resource filter that will run.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResourceExecutedContext), ResourceExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after <see cref="IResourceFilter.OnResourceExecuted(ResourceExecutedContext)"/>.
/// </summary>
public sealed class AfterResourceFilterOnResourceExecutedEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnResourceExecuted";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterResourceFilterOnResourceExecutedEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resourceExecutedContext">The <see cref="ResourceExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public AfterResourceFilterOnResourceExecutedEventData(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResourceExecutedContext = resourceExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The resource context.
    /// </summary>
    public ResourceExecutedContext ResourceExecutedContext { get; }

    /// <summary>
    /// The filter.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResourceExecutedContext), ResourceExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before <see cref="IExceptionFilter.OnException(ExceptionContext)"/>.
/// </summary>
public sealed class BeforeExceptionFilterOnException : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnException";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeExceptionFilterOnException"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="exceptionContext">The <see cref="ExceptionContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public BeforeExceptionFilterOnException(ActionDescriptor actionDescriptor, ExceptionContext exceptionContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ExceptionContext = exceptionContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ExceptionContext ExceptionContext { get; }

    /// <summary>
    /// The exception filter that will run.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ExceptionContext), ExceptionContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after <see cref="IExceptionFilter.OnException(ExceptionContext)"/>.
/// </summary>
public sealed class AfterExceptionFilterOnExceptionEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnException";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterExceptionFilterOnExceptionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="exceptionContext">The <see cref="ExceptionContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public AfterExceptionFilterOnExceptionEventData(ActionDescriptor actionDescriptor, ExceptionContext exceptionContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ExceptionContext = exceptionContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The exception context.
    /// </summary>
    public ExceptionContext ExceptionContext { get; }

    /// <summary>
    /// The exception filter that ran.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ExceptionContext), ExceptionContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before <see cref="IActionFilter"/> execution.
/// </summary>
public sealed class BeforeActionFilterOnActionExecutionEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnActionExecution";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeActionFilterOnActionExecutionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="actionExecutingContext">The <see cref="ActionExecutingContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public BeforeActionFilterOnActionExecutionEventData(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ActionExecutingContext = actionExecutingContext;
        Filter = filter;
    }

    /// <summary>
    /// The action that will run..
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The action context.
    /// </summary>
    public ActionExecutingContext ActionExecutingContext { get; }

    /// <summary>
    /// The action filter that will run.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ActionExecutingContext), ActionExecutingContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after <see cref="IActionFilter"/> execution.
/// </summary>
public sealed class AfterActionFilterOnActionExecutionEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnActionExecution";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterActionFilterOnActionExecutionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="actionExecutedContext">The <see cref="ActionExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public AfterActionFilterOnActionExecutionEventData(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ActionExecutedContext = actionExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The action that ran.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The action executed context.
    /// </summary>
    public ActionExecutedContext ActionExecutedContext { get; }

    /// <summary>
    /// The action filter that ran.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ActionExecutedContext), ActionExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before <see cref="IActionFilter.OnActionExecuting(ActionExecutingContext)"/>.
/// </summary>
public sealed class BeforeActionFilterOnActionExecutingEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnActionExecuting";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeActionFilterOnActionExecutingEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="actionExecutingContext">The <see cref="ActionExecutingContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public BeforeActionFilterOnActionExecutingEventData(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ActionExecutingContext = actionExecutingContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The action context.
    /// </summary>
    public ActionExecutingContext ActionExecutingContext { get; }

    /// <summary>
    /// The action filter that will run.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ActionExecutingContext), ActionExecutingContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after <see cref="IActionFilter.OnActionExecuting(ActionExecutingContext)"/>.
/// </summary>
public sealed class AfterActionFilterOnActionExecutingEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnActionExecuting";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterActionFilterOnActionExecutingEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="actionExecutingContext">The <see cref="ActionExecutingContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public AfterActionFilterOnActionExecutingEventData(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ActionExecutingContext = actionExecutingContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ActionExecutingContext ActionExecutingContext { get; }

    /// <summary>
    /// The action filter.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ActionExecutingContext), ActionExecutingContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before <see cref="IActionFilter.OnActionExecuted(ActionExecutedContext)"/>.
/// </summary>
public sealed class BeforeActionFilterOnActionExecutedEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnActionExecuted";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeActionFilterOnActionExecutedEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="actionExecutedContext">The <see cref="ActionExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public BeforeActionFilterOnActionExecutedEventData(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ActionExecutedContext = actionExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ActionExecutedContext ActionExecutedContext { get; }

    /// <summary>
    /// The action filter.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ActionExecutedContext), ActionExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after <see cref="IActionFilter.OnActionExecuted(ActionExecutedContext)"/>.
/// </summary>
public sealed class AfterActionFilterOnActionExecutedEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnActionExecuted";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterActionFilterOnActionExecutedEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="actionExecutedContext">The <see cref="ActionExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public AfterActionFilterOnActionExecutedEventData(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ActionExecutedContext = actionExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ActionExecutedContext ActionExecutedContext { get; }

    /// <summary>
    /// The action filter.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ActionExecutedContext), ActionExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before an controller action method.
/// </summary>
public sealed class BeforeControllerActionMethodEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeControllerActionMethod";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeControllerActionMethodEventData"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="actionArguments">The arguments to the action.</param>
    /// <param name="controller">The controller.</param>
    public BeforeControllerActionMethodEventData(ActionContext actionContext, IReadOnlyDictionary<string, object> actionArguments, object controller)
    {
        ActionContext = actionContext;
        ActionArguments = actionArguments;
        Controller = controller;
    }

    /// <summary>
    /// The action context.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// The action arguments.
    /// </summary>
    public IReadOnlyDictionary<string, object> ActionArguments { get; }

    /// <summary>
    /// The controller.
    /// </summary>
    public object Controller { get; }

    /// <inheritdoc/>
    protected sealed override int Count => 3;

    /// <inheritdoc/>
    protected sealed override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
        1 => new KeyValuePair<string, object>(nameof(ActionArguments), ActionArguments),
        2 => new KeyValuePair<string, object>(nameof(Controller), Controller),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after an controller action method.
/// </summary>
public sealed class AfterControllerActionMethodEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterControllerActionMethod";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterControllerActionMethodEventData"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="arguments">The arguments to the action.</param>
    /// <param name="controller">The controller.</param>
    /// <param name="result">The <see cref="IActionResult"/>.</param>
    public AfterControllerActionMethodEventData(ActionContext actionContext, IReadOnlyDictionary<string, object> arguments, object controller, IActionResult result)
    {
        ActionContext = actionContext;
        Arguments = arguments;
        Controller = controller;
        Result = result;
    }

    /// <summary>
    /// The context.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// The arguments.
    /// </summary>
    public IReadOnlyDictionary<string, object> Arguments { get; }

    /// <summary>
    /// The controller.
    /// </summary>
    public object Controller { get; }

    /// <summary>
    /// The result.
    /// </summary>
    public IActionResult Result { get; }

    /// <inheritdoc/>
    protected override int Count => 4;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
        1 => new KeyValuePair<string, object>(nameof(Controller), Controller),
        2 => new KeyValuePair<string, object>(nameof(Controller), Controller),
        3 => new KeyValuePair<string, object>(nameof(Result), Result),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before a ResultFilter's OnResultExecution
/// </summary>
public sealed class BeforeResultFilterOnResultExecutionEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnResultExecution";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeResultFilterOnResultExecutionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resultExecutingContext">The <see cref="ResultExecutingContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public BeforeResultFilterOnResultExecutionEventData(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResultExecutingContext = resultExecutingContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ResultExecutingContext ResultExecutingContext { get; }

    /// <summary>
    /// The result filter.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResultExecutingContext), ResultExecutingContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after a ResultFilter's OnResultExecution
/// </summary>
public sealed class AfterResultFilterOnResultExecutionEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnResultExecution";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterResultFilterOnResultExecutionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resultExecutedContext">The <see cref="ResultExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public AfterResultFilterOnResultExecutionEventData(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResultExecutedContext = resultExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ResultExecutedContext ResultExecutedContext { get; }

    /// <summary>
    /// The result filter.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResultExecutedContext), ResultExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before <see cref="IResultFilter.OnResultExecuting(ResultExecutingContext)"/>.
/// </summary>
public sealed class BeforeResultFilterOnResultExecutingEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnResultExecuting";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeResultFilterOnResultExecutingEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resultExecutingContext">The <see cref="ResultExecutingContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public BeforeResultFilterOnResultExecutingEventData(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResultExecutingContext = resultExecutingContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ResultExecutingContext ResultExecutingContext { get; }

    /// <summary>
    /// The result filter.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResultExecutingContext), ResultExecutingContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after <see cref="IResultFilter.OnResultExecuting(ResultExecutingContext)"/>.
/// </summary>
public sealed class AfterResultFilterOnResultExecutingEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnResultExecuting";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterResultFilterOnResultExecutedEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resultExecutingContext">The <see cref="ResultExecutingContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public AfterResultFilterOnResultExecutingEventData(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResultExecutingContext = resultExecutingContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ResultExecutingContext ResultExecutingContext { get; }

    /// <summary>
    /// The filter.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResultExecutingContext), ResultExecutingContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before <see cref="IResultFilter.OnResultExecuted(ResultExecutedContext)"/>.
/// </summary>
public sealed class BeforeResultFilterOnResultExecutedEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnResultExecuted";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeResultFilterOnResultExecutedEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resultExecutedContext">The <see cref="ResultExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public BeforeResultFilterOnResultExecutedEventData(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResultExecutedContext = resultExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The context.
    /// </summary>
    public ResultExecutedContext ResultExecutedContext { get; }

    /// <summary>
    /// The result filter.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResultExecutedContext), ResultExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after <see cref="IResultFilter.OnResultExecuted(ResultExecutedContext)"/>.
/// </summary>
public sealed class AfterResultFilterOnResultExecutedEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnResultExecuted";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterResultFilterOnResultExecutedEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor"/>.</param>
    /// <param name="resultExecutedContext">The <see cref="ResultExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
    public AfterResultFilterOnResultExecutedEventData(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
    {
        ActionDescriptor = actionDescriptor;
        ResultExecutedContext = resultExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The action.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The result executed context.
    /// </summary>
    public ResultExecutedContext ResultExecutedContext { get; }

    /// <summary>
    /// The filter that ran.
    /// </summary>
    public IFilterMetadata Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(ResultExecutedContext), ResultExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before an action result is invoked.
/// </summary>
public sealed class BeforeActionResultEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeActionResult";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeActionResultEventData"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="result">The <see cref="IActionResult"/>.</param>
    public BeforeActionResultEventData(ActionContext actionContext, IActionResult result)
    {
        ActionContext = actionContext;
        Result = result;
    }

    /// <summary>
    /// The action context.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// The action result.
    /// </summary>
    public IActionResult Result { get; }

    /// <inheritdoc/>
    protected override int Count => 2;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
        1 => new KeyValuePair<string, object>(nameof(Result), Result),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after an action result is invoked.
/// </summary>
public sealed class AfterActionResultEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterActionResult";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterActionResultEventData"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="result">The <see cref="IActionResult"/>.</param>
    public AfterActionResultEventData(ActionContext actionContext, IActionResult result)
    {
        ActionContext = actionContext;
        Result = result;
    }

    /// <summary>
    /// The action context.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// The result.
    /// </summary>
    public IActionResult Result { get; }

    /// <inheritdoc/>
    protected override int Count => 2;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
        1 => new KeyValuePair<string, object>(nameof(Result), Result),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}
