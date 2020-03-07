// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public sealed class BeforeHandlerMethodEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeHandlerMethod";

        public BeforeHandlerMethodEventData(ActionContext actionContext, IReadOnlyDictionary<string, object> arguments, HandlerMethodDescriptor handlerMethodDescriptor, object instance)
        {
            ActionContext = actionContext;
            Arguments = arguments;
            HandlerMethodDescriptor = handlerMethodDescriptor;
            Instance = instance;
        }

        public ActionContext ActionContext { get; }
        public IReadOnlyDictionary<string, object> Arguments { get; }
        public HandlerMethodDescriptor HandlerMethodDescriptor { get; }
        public object Instance { get; }

        protected override int Count => 4;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(Arguments), Arguments),
            2 => new KeyValuePair<string, object>(nameof(HandlerMethodDescriptor), HandlerMethodDescriptor),
            3 => new KeyValuePair<string, object>(nameof(Instance), Instance),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterHandlerMethodEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterHandlerMethod";

        public AfterHandlerMethodEventData(ActionContext actionContext, IReadOnlyDictionary<string, object> arguments, HandlerMethodDescriptor handlerMethodDescriptor, object instance, IActionResult result)
        {
            ActionContext = actionContext;
            Arguments = arguments;
            HandlerMethodDescriptor = handlerMethodDescriptor;
            Instance = instance;
            Result = result;
        }

        public ActionContext ActionContext { get; }
        public IReadOnlyDictionary<string, object> Arguments { get; }
        public HandlerMethodDescriptor HandlerMethodDescriptor { get; }
        public object Instance { get; }
        public IActionResult Result { get; }

        protected override int Count => 5;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(Arguments), Arguments),
            2 => new KeyValuePair<string, object>(nameof(HandlerMethodDescriptor), HandlerMethodDescriptor),
            3 => new KeyValuePair<string, object>(nameof(Instance), Instance),
            4 => new KeyValuePair<string, object>(nameof(Result), Result),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforePageFilterOnPageHandlerExecutionEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnPageHandlerExecution";

        public BeforePageFilterOnPageHandlerExecutionEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutingContext handlerExecutionContext, IAsyncPageFilter filter)
        {
            ActionDescriptor = actionDescriptor;
            HandlerExecutionContext = handlerExecutionContext;
            Filter = filter;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }
        public PageHandlerExecutingContext HandlerExecutionContext { get; }
        public IAsyncPageFilter Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HandlerExecutionContext), HandlerExecutionContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterPageFilterOnPageHandlerExecutionEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnPageHandlerExecution";

        public AfterPageFilterOnPageHandlerExecutionEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutedContext handlerExecutedContext, IAsyncPageFilter filter)
        {
            ActionDescriptor = actionDescriptor;
            HandlerExecutedContext = handlerExecutedContext;
            Filter = filter;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }
        public PageHandlerExecutedContext HandlerExecutedContext { get; }
        public IAsyncPageFilter Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HandlerExecutedContext), HandlerExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforePageFilterOnPageHandlerExecutingEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnPageHandlerExecuting";

        public BeforePageFilterOnPageHandlerExecutingEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutingContext handlerExecutingContext, IPageFilter filter)
        {
            ActionDescriptor = actionDescriptor;
            HandlerExecutingContext = handlerExecutingContext;
            Filter = filter;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }
        public PageHandlerExecutingContext HandlerExecutingContext { get; }
        public IPageFilter Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HandlerExecutingContext), HandlerExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterPageFilterOnPageHandlerExecutingEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnPageHandlerExecuting";

        public AfterPageFilterOnPageHandlerExecutingEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutingContext handlerExecutingContext, IPageFilter filter)
        {
            ActionDescriptor = actionDescriptor;
            HandlerExecutingContext = handlerExecutingContext;
            Filter = filter;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }
        public PageHandlerExecutingContext HandlerExecutingContext { get; }
        public IPageFilter Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HandlerExecutingContext), HandlerExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforePageFilterOnPageHandlerExecutedEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnPageHandlerExecuted";

        public BeforePageFilterOnPageHandlerExecutedEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutedContext handlerExecutedContext, IPageFilter filter)
        {
            ActionDescriptor = actionDescriptor;
            HandlerExecutedContext = handlerExecutedContext;
            Filter = filter;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }
        public PageHandlerExecutedContext HandlerExecutedContext { get; }
        public IPageFilter Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HandlerExecutedContext), HandlerExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterPageFilterOnPageHandlerExecutedEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnPageHandlerExecuted";

        public AfterPageFilterOnPageHandlerExecutedEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerExecutedContext handlerExecutedContext, IPageFilter filter)
        {
            ActionDescriptor = actionDescriptor;
            HandlerExecutedContext = handlerExecutedContext;
            Filter = filter;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }
        public PageHandlerExecutedContext HandlerExecutedContext { get; }
        public IPageFilter Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HandlerExecutedContext), HandlerExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforePageFilterOnPageHandlerSelectionEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnPageHandlerSelection";

        public BeforePageFilterOnPageHandlerSelectionEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerSelectedContext handlerSelectedContext, IAsyncPageFilter filter)
        {
            ActionDescriptor = actionDescriptor;
            HandlerSelectedContext = handlerSelectedContext;
            Filter = filter;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }
        public PageHandlerSelectedContext HandlerSelectedContext { get; }
        public IAsyncPageFilter Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HandlerSelectedContext), HandlerSelectedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterPageFilterOnPageHandlerSelectionEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnPageHandlerSelection";

        public AfterPageFilterOnPageHandlerSelectionEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerSelectedContext handlerSelectedContext, IAsyncPageFilter filter)
        {
            ActionDescriptor = actionDescriptor;
            HandlerSelectedContext = handlerSelectedContext;
            Filter = filter;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }
        public PageHandlerSelectedContext HandlerSelectedContext { get; }
        public IAsyncPageFilter Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HandlerSelectedContext), HandlerSelectedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforePageFilterOnPageHandlerSelectedEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnPageHandlerSelected";

        public BeforePageFilterOnPageHandlerSelectedEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerSelectedContext handlerSelectedContext, IPageFilter filter)
        {
            ActionDescriptor = actionDescriptor;
            HandlerSelectedContext = handlerSelectedContext;
            Filter = filter;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }
        public PageHandlerSelectedContext HandlerSelectedContext { get; }
        public IPageFilter Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HandlerSelectedContext), HandlerSelectedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterPageFilterOnPageHandlerSelectedEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnPageHandlerSelected";

        public AfterPageFilterOnPageHandlerSelectedEventData(CompiledPageActionDescriptor actionDescriptor, PageHandlerSelectedContext handlerSelectedContext, IPageFilter filter)
        {
            ActionDescriptor = actionDescriptor;
            HandlerSelectedContext = handlerSelectedContext;
            Filter = filter;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }
        public PageHandlerSelectedContext HandlerSelectedContext { get; }
        public IPageFilter Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HandlerSelectedContext), HandlerSelectedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }
}