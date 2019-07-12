// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public sealed class BeforeAction : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeAction);
        public BeforeAction(ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData)
        {
            ActionDescriptor = actionDescriptor;
            HttpContext = httpContext;
            RouteData = routeData;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public HttpContext HttpContext { get; }
        public RouteData RouteData { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
            2 => new KeyValuePair<string, object>(nameof(RouteData), RouteData),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterAction : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterAction);

        public AfterAction(ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData)
        {
            ActionDescriptor = actionDescriptor;
            HttpContext = httpContext;
            RouteData = routeData;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public HttpContext HttpContext { get; }
        public RouteData RouteData { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
            2 => new KeyValuePair<string, object>(nameof(RouteData), RouteData),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnAuthorization : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeOnAuthorization);

        public BeforeOnAuthorization(ActionDescriptor actionDescriptor, AuthorizationFilterContext authorizationContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            AuthorizationContext = authorizationContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public AuthorizationFilterContext AuthorizationContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(AuthorizationContext), AuthorizationContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnAuthorization : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterOnAuthorization);

        public AfterOnAuthorization(ActionDescriptor actionDescriptor, AuthorizationFilterContext authorizationContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            AuthorizationContext = authorizationContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public AuthorizationFilterContext AuthorizationContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(AuthorizationContext), AuthorizationContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResourceExecution : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResourceExecution);

        public BeforeOnResourceExecution(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutingContext = resourceExecutingContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutingContext ResourceExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutingContext), ResourceExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResourceExecution : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterOnResourceExecution);

        public AfterOnResourceExecution(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutedContext = resourceExecutedContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutedContext ResourceExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutedContext), ResourceExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResourceExecuting : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResourceExecuting);

        public BeforeOnResourceExecuting(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutingContext = resourceExecutingContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutingContext ResourceExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutingContext), ResourceExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResourceExecuting : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterOnResourceExecuting);

        public AfterOnResourceExecuting(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutingContext = resourceExecutingContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutingContext ResourceExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutingContext), ResourceExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResourceExecuted : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResourceExecuted);

        public BeforeOnResourceExecuted(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutedContext = resourceExecutedContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutedContext ResourceExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutedContext), ResourceExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResourceExecuted : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterOnResourceExecuted);

        public AfterOnResourceExecuted(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutedContext = resourceExecutedContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutedContext ResourceExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutedContext), ResourceExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnException : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeOnException);

        public BeforeOnException(ActionDescriptor actionDescriptor, ExceptionContext exceptionContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ExceptionContext = exceptionContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ExceptionContext ExceptionContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ExceptionContext), ExceptionContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnException : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterOnException);

        public AfterOnException(ActionDescriptor actionDescriptor, ExceptionContext exceptionContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ExceptionContext = exceptionContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ExceptionContext ExceptionContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ExceptionContext), ExceptionContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnActionExecution : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeOnActionExecution);

        public BeforeOnActionExecution(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutingContext = actionExecutingContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutingContext ActionExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutingContext), ActionExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnActionExecution : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterOnActionExecution);

        public AfterOnActionExecution(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutedContext = actionExecutedContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutedContext ActionExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutedContext), ActionExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnActionExecuting : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeOnActionExecuting);

        public BeforeOnActionExecuting(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutingContext = actionExecutingContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutingContext ActionExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutingContext), ActionExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnActionExecuting : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterOnActionExecuting);

        public AfterOnActionExecuting(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutingContext = actionExecutingContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutingContext ActionExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutingContext), ActionExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnActionExecuted : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeOnActionExecuted);

        public BeforeOnActionExecuted(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutedContext = actionExecutedContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutedContext ActionExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutedContext), ActionExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnActionExecuted : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterOnActionExecuted);

        public AfterOnActionExecuted(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutedContext = actionExecutedContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutedContext ActionExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutedContext), ActionExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeActionMethod : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeActionMethod);

        public BeforeActionMethod(ActionContext actionContext, IReadOnlyDictionary<string, object> arguments, object controller)
        {
            ActionContext = actionContext;
            Arguments = arguments;
            Controller = controller;
        }

        public ActionContext ActionContext { get; }
        public IReadOnlyDictionary<string, object> Arguments { get; }
        public object Controller { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(Arguments), Arguments),
            2 => new KeyValuePair<string, object>(nameof(Controller), Controller),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterActionMethod : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterActionMethod);

        public AfterActionMethod(ActionContext actionContext, IReadOnlyDictionary<string, object> arguments, object controller, IActionResult result)
        {
            ActionContext = actionContext;
            Arguments = arguments;
            Controller = controller;
            Result = result;
        }

        public ActionContext ActionContext { get; }
        public IReadOnlyDictionary<string, object> Arguments { get; }
        public object Controller { get; }
        public IActionResult Result { get; }

        protected override int Count => 4;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(Controller), Controller),
            2 => new KeyValuePair<string, object>(nameof(Controller), Controller),
            3 => new KeyValuePair<string, object>(nameof(Result), Result),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResultExecution : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResultExecution);

        public BeforeOnResultExecution(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutingContext = resultExecutingContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutingContext ResultExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutingContext), ResultExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResultExecution : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterOnResultExecution);

        public AfterOnResultExecution(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutedContext = resultExecutedContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutedContext ResultExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutedContext), ResultExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResultExecuting : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResultExecuting);

        public BeforeOnResultExecuting(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutingContext = resultExecutingContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutingContext ResultExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutingContext), ResultExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResultExecuting : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterOnResultExecuting);

        public AfterOnResultExecuting(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutingContext = resultExecutingContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutingContext ResultExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutingContext), ResultExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResultExecuted : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResultExecuted);

        public BeforeOnResultExecuted(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutedContext = resultExecutedContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutedContext ResultExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutedContext), ResultExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResultExecuted : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterOnResultExecuted);

        public AfterOnResultExecuted(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutedContext = resultExecutedContext;
            Filter = filter;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutedContext ResultExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutedContext), ResultExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeActionResult : EventData
    {
        public const string EventName = EventNamespace + nameof(BeforeActionResult);

        public BeforeActionResult(ActionContext actionContext, IActionResult result)
        {
            ActionContext = actionContext;
            Result = result;
        }

        public ActionContext ActionContext { get; }
        public IActionResult Result { get; }

        protected override int Count => 2;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(Result), Result),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterActionResult : EventData
    {
        public const string EventName = EventNamespace + nameof(AfterActionResult);

        public AfterActionResult(ActionContext actionContext, IActionResult result)
        {
            ActionContext = actionContext;
            Result = result;
        }

        public ActionContext ActionContext { get; }
        public IActionResult Result { get; }

        protected override int Count => 2;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(Result), Result),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }
}