// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Diagnostics;

/// <summary>
/// An <see cref="EventData"/> that occurs before a handler method is called.
/// </summary>
public sealed class BeforeHandlerMethodEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeHandlerMethod";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeHandlerMethodEventData"/>.
    /// </summary>
    /// <param name="actionContext">The action context.</param>
    /// <param name="arguments">The arguments to the method.</param>
    /// <param name="handlerMethodDescriptor">The method descriptor.</param>
    /// <param name="instance">The instance.</param>
    public BeforeHandlerMethodEventData(ActionContext actionContext, IReadOnlyDictionary<string, object?> arguments, HandlerMethodDescriptor handlerMethodDescriptor, object instance)
    {
        ActionContext = actionContext;
        Arguments = arguments;
        HandlerMethodDescriptor = handlerMethodDescriptor;
        Instance = instance;
    }

    /// <summary>
    /// The <see cref="ActionContext"/>.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// The arguments to the method.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; }

    /// <summary>
    /// The <see cref="HandlerMethodDescriptor"/>.
    /// </summary>
    public HandlerMethodDescriptor HandlerMethodDescriptor { get; }

    /// <summary>
    /// The instance.
    /// </summary>
    public object Instance { get; }

    /// <inheritdoc/>
    protected override int Count => 4;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
        1 => new KeyValuePair<string, object>(nameof(Arguments), Arguments),
        2 => new KeyValuePair<string, object>(nameof(HandlerMethodDescriptor), HandlerMethodDescriptor),
        3 => new KeyValuePair<string, object>(nameof(Instance), Instance),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after a handler method is called.
/// </summary>
public sealed class AfterHandlerMethodEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterHandlerMethod";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterHandlerMethodEventData"/>.
    /// </summary>
    /// <param name="actionContext">The action context.</param>
    /// <param name="arguments">The arguments to the method.</param>
    /// <param name="handlerMethodDescriptor">The method descriptor.</param>
    /// <param name="instance">The instance.</param>
    /// <param name="result">The result of the handler method</param>
    public AfterHandlerMethodEventData(ActionContext actionContext, IReadOnlyDictionary<string, object?> arguments, HandlerMethodDescriptor handlerMethodDescriptor, object instance, IActionResult? result)
    {
        ActionContext = actionContext;
        Arguments = arguments;
        HandlerMethodDescriptor = handlerMethodDescriptor;
        Instance = instance;
        Result = result;
    }

    /// <summary>
    /// The <see cref="ActionContext"/>.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// The arguments to the method.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; }

    /// <summary>
    /// The <see cref="HandlerMethodDescriptor"/>.
    /// </summary>
    public HandlerMethodDescriptor HandlerMethodDescriptor { get; }

    /// <summary>
    /// The instance.
    /// </summary>
    public object Instance { get; }

    /// <summary>
    /// The result of the method.
    /// </summary>
    public IActionResult? Result { get; }

    /// <inheritdoc/>
    protected override int Count => 5;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
        1 => new KeyValuePair<string, object>(nameof(Arguments), Arguments),
        2 => new KeyValuePair<string, object>(nameof(HandlerMethodDescriptor), HandlerMethodDescriptor),
        3 => new KeyValuePair<string, object>(nameof(Instance), Instance),
        4 => new KeyValuePair<string, object>(nameof(Result), Result!),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before page handler execution.
/// </summary>
public sealed class BeforePageFilterOnPageHandlerExecutionEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnPageHandlerExecution";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterHandlerMethodEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <param name="handlerExecutionContext">The <see cref="HandlerExecutionContext"/>.</param>
    /// <param name="filter">The <see cref="IAsyncPageFilter"/>.</param>
    public BeforePageFilterOnPageHandlerExecutionEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutingContext handlerExecutionContext, IAsyncPageFilter filter)
    {
        ActionDescriptor = actionDescriptor;
        HandlerExecutionContext = handlerExecutionContext;
        Filter = filter;
    }

    /// <summary>
    /// The <see cref="CompiledPageActionDescriptor"/>.
    /// </summary>
    public CompiledPageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="PageHandlerExecutingContext"/>.
    /// </summary>
    public PageHandlerExecutingContext HandlerExecutionContext { get; }

    /// <summary>
    /// The <see cref="IAsyncPageFilter"/>.
    /// </summary>
    public IAsyncPageFilter Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HandlerExecutionContext), HandlerExecutionContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after page handler execution.
/// </summary>
public sealed class AfterPageFilterOnPageHandlerExecutionEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnPageHandlerExecution";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterPageFilterOnPageHandlerExecutionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <param name="handlerExecutedContext">The <see cref="HandlerExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IAsyncPageFilter"/>.</param>
    public AfterPageFilterOnPageHandlerExecutionEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutedContext handlerExecutedContext, IAsyncPageFilter filter)
    {
        ActionDescriptor = actionDescriptor;
        HandlerExecutedContext = handlerExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The <see cref="CompiledPageActionDescriptor"/>.
    /// </summary>
    public CompiledPageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="PageHandlerExecutedContext"/>.
    /// </summary>
    public PageHandlerExecutedContext HandlerExecutedContext { get; }

    /// <summary>
    /// The <see cref="IAsyncPageFilter"/>.
    /// </summary>
    public IAsyncPageFilter Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HandlerExecutedContext), HandlerExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before page handler executing.
/// </summary>
public sealed class BeforePageFilterOnPageHandlerExecutingEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnPageHandlerExecuting";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforePageFilterOnPageHandlerExecutingEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <param name="handlerExecutingContext">The <see cref="PageHandlerExecutingContext"/>.</param>
    /// <param name="filter">The <see cref="IPageFilter"/>.</param>
    public BeforePageFilterOnPageHandlerExecutingEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutingContext handlerExecutingContext, IPageFilter filter)
    {
        ActionDescriptor = actionDescriptor;
        HandlerExecutingContext = handlerExecutingContext;
        Filter = filter;
    }

    /// <summary>
    /// The <see cref="CompiledPageActionDescriptor"/>.
    /// </summary>
    public CompiledPageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="PageHandlerExecutingContext"/>.
    /// </summary>
    public PageHandlerExecutingContext HandlerExecutingContext { get; }

    /// <summary>
    /// The <see cref="IAsyncPageFilter"/>.
    /// </summary>
    public IPageFilter Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HandlerExecutingContext), HandlerExecutingContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after page handler executing.
/// </summary>
public sealed class AfterPageFilterOnPageHandlerExecutingEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnPageHandlerExecuting";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterPageFilterOnPageHandlerExecutingEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <param name="handlerExecutingContext">The <see cref="PageHandlerExecutingContext"/>.</param>
    /// <param name="filter">The <see cref="IPageFilter"/>.</param>
    public AfterPageFilterOnPageHandlerExecutingEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutingContext handlerExecutingContext, IPageFilter filter)
    {
        ActionDescriptor = actionDescriptor;
        HandlerExecutingContext = handlerExecutingContext;
        Filter = filter;
    }

    /// <summary>
    /// The <see cref="CompiledPageActionDescriptor"/>.
    /// </summary>
    public CompiledPageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="PageHandlerExecutingContext"/>.
    /// </summary>
    public PageHandlerExecutingContext HandlerExecutingContext { get; }

    /// <summary>
    /// The <see cref="IPageFilter"/>.
    /// </summary>
    public IPageFilter Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HandlerExecutingContext), HandlerExecutingContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before page handler executed.
/// </summary>
public sealed class BeforePageFilterOnPageHandlerExecutedEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnPageHandlerExecuted";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforePageFilterOnPageHandlerExecutedEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <param name="handlerExecutedContext">The <see cref="PageHandlerExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IPageFilter"/>.</param>
    public BeforePageFilterOnPageHandlerExecutedEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutedContext handlerExecutedContext, IPageFilter filter)
    {
        ActionDescriptor = actionDescriptor;
        HandlerExecutedContext = handlerExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The <see cref="CompiledPageActionDescriptor"/>.
    /// </summary>
    public CompiledPageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="PageHandlerExecutedContext"/>.
    /// </summary>
    public PageHandlerExecutedContext HandlerExecutedContext { get; }

    /// <summary>
    /// The <see cref="IPageFilter"/>.
    /// </summary>
    public IPageFilter Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HandlerExecutedContext), HandlerExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after page handler executed.
/// </summary>
public sealed class AfterPageFilterOnPageHandlerExecutedEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnPageHandlerExecuted";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterPageFilterOnPageHandlerExecutedEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <param name="handlerExecutedContext">The <see cref="PageHandlerExecutedContext"/>.</param>
    /// <param name="filter">The <see cref="IPageFilter"/>.</param>
    public AfterPageFilterOnPageHandlerExecutedEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutedContext handlerExecutedContext, IPageFilter filter)
    {
        ActionDescriptor = actionDescriptor;
        HandlerExecutedContext = handlerExecutedContext;
        Filter = filter;
    }

    /// <summary>
    /// The <see cref="CompiledPageActionDescriptor"/>.
    /// </summary>
    public CompiledPageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="PageHandlerExecutedContext"/>.
    /// </summary>
    public PageHandlerExecutedContext HandlerExecutedContext { get; }

    /// <summary>
    /// The <see cref="IPageFilter"/>.
    /// </summary>
    public IPageFilter Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HandlerExecutedContext), HandlerExecutedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before page handler selection.
/// </summary>
public sealed class BeforePageFilterOnPageHandlerSelectionEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnPageHandlerSelection";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforePageFilterOnPageHandlerSelectionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <param name="handlerSelectedContext">The <see cref="PageHandlerSelectedContext"/>.</param>
    /// <param name="filter">The <see cref="IAsyncPageFilter"/>.</param>
    public BeforePageFilterOnPageHandlerSelectionEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerSelectedContext handlerSelectedContext, IAsyncPageFilter filter)
    {
        ActionDescriptor = actionDescriptor;
        HandlerSelectedContext = handlerSelectedContext;
        Filter = filter;
    }

    /// <summary>
    /// The <see cref="CompiledPageActionDescriptor"/>.
    /// </summary>
    public CompiledPageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="PageHandlerSelectedContext"/>.
    /// </summary>
    public PageHandlerSelectedContext HandlerSelectedContext { get; }

    /// <summary>
    /// The <see cref="IPageFilter"/>.
    /// </summary>
    public IAsyncPageFilter Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HandlerSelectedContext), HandlerSelectedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after page handler selection.
/// </summary>
public sealed class AfterPageFilterOnPageHandlerSelectionEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnPageHandlerSelection";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterPageFilterOnPageHandlerSelectionEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <param name="handlerSelectedContext">The <see cref="PageHandlerSelectedContext"/>.</param>
    /// <param name="filter">The <see cref="IAsyncPageFilter"/>.</param>
    public AfterPageFilterOnPageHandlerSelectionEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerSelectedContext handlerSelectedContext, IAsyncPageFilter filter)
    {
        ActionDescriptor = actionDescriptor;
        HandlerSelectedContext = handlerSelectedContext;
        Filter = filter;
    }

    /// <summary>
    /// The <see cref="CompiledPageActionDescriptor"/>.
    /// </summary>
    public CompiledPageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="PageHandlerSelectedContext"/>.
    /// </summary>
    public PageHandlerSelectedContext HandlerSelectedContext { get; }

    /// <summary>
    /// The <see cref="IAsyncPageFilter"/>.
    /// </summary>
    public IAsyncPageFilter Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HandlerSelectedContext), HandlerSelectedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs before <see cref="IPageFilter.OnPageHandlerSelected(PageHandlerSelectedContext)"/>.
/// </summary>
public sealed class BeforePageFilterOnPageHandlerSelectedEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "BeforeOnPageHandlerSelected";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforePageFilterOnPageHandlerSelectedEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <param name="handlerSelectedContext">The <see cref="PageHandlerSelectedContext"/>.</param>
    /// <param name="filter">The <see cref="IPageFilter"/>.</param>
    public BeforePageFilterOnPageHandlerSelectedEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerSelectedContext handlerSelectedContext, IPageFilter filter)
    {
        ActionDescriptor = actionDescriptor;
        HandlerSelectedContext = handlerSelectedContext;
        Filter = filter;
    }

    /// <summary>
    /// The <see cref="CompiledPageActionDescriptor"/>.
    /// </summary>
    public CompiledPageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="PageHandlerSelectedContext"/>.
    /// </summary>
    public PageHandlerSelectedContext HandlerSelectedContext { get; }

    /// <summary>
    /// The <see cref="IPageFilter"/>.
    /// </summary>
    public IPageFilter Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HandlerSelectedContext), HandlerSelectedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after <see cref="IPageFilter.OnPageHandlerSelected(PageHandlerSelectedContext)"/>.
/// </summary>
public sealed class AfterPageFilterOnPageHandlerSelectedEventData : EventData
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public const string EventName = EventNamespace + "AfterOnPageHandlerSelected";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterPageFilterOnPageHandlerSelectedEventData"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="CompiledPageActionDescriptor"/>.</param>
    /// <param name="handlerSelectedContext">The <see cref="PageHandlerSelectedContext"/>.</param>
    /// <param name="filter">The <see cref="IPageFilter"/>.</param>
    public AfterPageFilterOnPageHandlerSelectedEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerSelectedContext handlerSelectedContext, IPageFilter filter)
    {
        ActionDescriptor = actionDescriptor;
        HandlerSelectedContext = handlerSelectedContext;
        Filter = filter;
    }

    /// <summary>
    /// The <see cref="CompiledPageActionDescriptor"/>.
    /// </summary>
    public CompiledPageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="PageHandlerSelectedContext"/>.
    /// </summary>
    public PageHandlerSelectedContext HandlerSelectedContext { get; }

    /// <summary>
    /// The <see cref="IPageFilter"/>.
    /// </summary>
    public IPageFilter Filter { get; }

    /// <inheritdoc/>
    protected override int Count => 3;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        1 => new KeyValuePair<string, object>(nameof(HandlerSelectedContext), HandlerSelectedContext),
        2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
        _ => throw new IndexOutOfRangeException(nameof(index))
    };
}
