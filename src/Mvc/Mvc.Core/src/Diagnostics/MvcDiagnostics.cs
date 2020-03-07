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
    public sealed class BeforeActionEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeAction";
        public BeforeActionEventData(ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData)
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

    public sealed class AfterActionEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterAction";

        public AfterActionEventData(ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData)
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

    public sealed class BeforeAuthorizationFilterOnAuthorizationEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnAuthorization";

        public BeforeAuthorizationFilterOnAuthorizationEventData(ActionDescriptor actionDescriptor, AuthorizationFilterContext authorizationContext, IFilterMetadata filter)
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

    public sealed class AfterAuthorizationFilterOnAuthorizationEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnAuthorization";

        public AfterAuthorizationFilterOnAuthorizationEventData(ActionDescriptor actionDescriptor, AuthorizationFilterContext authorizationContext, IFilterMetadata filter)
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

    public sealed class BeforeResourceFilterOnResourceExecutionEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnResourceExecution";

        public BeforeResourceFilterOnResourceExecutionEventData(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
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

    public sealed class AfterResourceFilterOnResourceExecutionEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnResourceExecution";

        public AfterResourceFilterOnResourceExecutionEventData(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
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

    public sealed class BeforeResourceFilterOnResourceExecutingEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnResourceExecuting";

        public BeforeResourceFilterOnResourceExecutingEventData(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
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

    public sealed class AfterResourceFilterOnResourceExecutingEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnResourceExecuting";

        public AfterResourceFilterOnResourceExecutingEventData(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
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

    public sealed class BeforeResourceFilterOnResourceExecutedEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnResourceExecuted";

        public BeforeResourceFilterOnResourceExecutedEventData(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
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

    public sealed class AfterResourceFilterOnResourceExecutedEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnResourceExecuted";

        public AfterResourceFilterOnResourceExecutedEventData(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
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

    public sealed class BeforeExceptionFilterOnException : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnException";

        public BeforeExceptionFilterOnException(ActionDescriptor actionDescriptor, ExceptionContext exceptionContext, IFilterMetadata filter)
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

    public sealed class AfterExceptionFilterOnExceptionEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnException";

        public AfterExceptionFilterOnExceptionEventData(ActionDescriptor actionDescriptor, ExceptionContext exceptionContext, IFilterMetadata filter)
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

    public sealed class BeforeActionFilterOnActionExecutionEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnActionExecution";

        public BeforeActionFilterOnActionExecutionEventData(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
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

    public sealed class AfterActionFilterOnActionExecutionEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnActionExecution";

        public AfterActionFilterOnActionExecutionEventData(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
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

    public sealed class BeforeActionFilterOnActionExecutingEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnActionExecuting";

        public BeforeActionFilterOnActionExecutingEventData(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
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

    public sealed class AfterActionFilterOnActionExecutingEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnActionExecuting";

        public AfterActionFilterOnActionExecutingEventData(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
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

    public sealed class BeforeActionFilterOnActionExecutedEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnActionExecuted";

        public BeforeActionFilterOnActionExecutedEventData(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
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

    public sealed class AfterActionFilterOnActionExecutedEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnActionExecuted";

        public AfterActionFilterOnActionExecutedEventData(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
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

    public sealed class BeforeControllerActionMethodEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeControllerActionMethod";

        public BeforeControllerActionMethodEventData(ActionContext actionContext, IReadOnlyDictionary<string, object> actionArguments, object controller)
        {
            ActionContext = actionContext;
            ActionArguments = actionArguments;
            Controller = controller;
        }

        public ActionContext ActionContext { get; }
        public IReadOnlyDictionary<string, object> ActionArguments { get; }
        public object Controller { get; }

        protected sealed override int Count => 3;

        protected sealed override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(ActionArguments), ActionArguments),
            2 => new KeyValuePair<string, object>(nameof(Controller), Controller),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterControllerActionMethodEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterControllerActionMethod";

        public AfterControllerActionMethodEventData(ActionContext actionContext, IReadOnlyDictionary<string, object> arguments, object controller, IActionResult result)
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

    public sealed class BeforeResultFilterOnResultExecutionEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnResultExecution";

        public BeforeResultFilterOnResultExecutionEventData(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
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

    public sealed class AfterResultFilterOnResultExecutionEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnResultExecution";

        public AfterResultFilterOnResultExecutionEventData(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
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

    public sealed class BeforeResultFilterOnResultExecutingEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnResultExecuting";

        public BeforeResultFilterOnResultExecutingEventData(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
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

    public sealed class AfterResultFilterOnResultExecutingEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnResultExecuting";

        public AfterResultFilterOnResultExecutingEventData(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
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

    public sealed class BeforeResultFilterOnResultExecutedEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeOnResultExecuted";

        public BeforeResultFilterOnResultExecutedEventData(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
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

    public sealed class AfterResultFilterOnResultExecutedEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterOnResultExecuted";

        public AfterResultFilterOnResultExecutedEventData(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
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

    public sealed class BeforeActionResultEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeActionResult";

        public BeforeActionResultEventData(ActionContext actionContext, IActionResult result)
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

    public sealed class AfterActionResultEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterActionResult";

        public AfterActionResultEventData(ActionContext actionContext, IActionResult result)
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