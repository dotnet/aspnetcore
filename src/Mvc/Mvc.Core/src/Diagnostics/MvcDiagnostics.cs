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
    public sealed class BeforeAction : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeAction);
        public ActionDescriptor ActionDescriptor { get; }
        public HttpContext HttpContext { get; }
        public RouteData RouteData { get; }

        public override int Count => 3;

        public BeforeAction(ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData)
        {
            ActionDescriptor = actionDescriptor;
            HttpContext = httpContext;
            RouteData = routeData;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
            2 => new KeyValuePair<string, object>(nameof(RouteData), RouteData),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterAction : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterAction);
        public ActionDescriptor ActionDescriptor { get; }
        public HttpContext HttpContext { get; }
        public RouteData RouteData { get; }

        public override int Count => 3;

        public AfterAction(ActionDescriptor actionDescriptor, HttpContext httpContext, RouteData routeData)
        {
            ActionDescriptor = actionDescriptor;
            HttpContext = httpContext;
            RouteData = routeData;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
            2 => new KeyValuePair<string, object>(nameof(RouteData), RouteData),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnAuthorization : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeOnAuthorization);
        public ActionDescriptor ActionDescriptor { get; }
        public AuthorizationFilterContext AuthorizationContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public BeforeOnAuthorization(ActionDescriptor actionDescriptor, AuthorizationFilterContext authorizationContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            AuthorizationContext = authorizationContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(AuthorizationContext), AuthorizationContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnAuthorization : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterOnAuthorization);
        public ActionDescriptor ActionDescriptor { get; }
        public AuthorizationFilterContext AuthorizationContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public AfterOnAuthorization(ActionDescriptor actionDescriptor, AuthorizationFilterContext authorizationContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            AuthorizationContext = authorizationContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(AuthorizationContext), AuthorizationContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResourceExecution : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResourceExecution);
        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutingContext ResourceExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public BeforeOnResourceExecution(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutingContext = resourceExecutingContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutingContext), ResourceExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResourceExecution : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterOnResourceExecution);
        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutedContext ResourceExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public AfterOnResourceExecution(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutedContext = resourceExecutedContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutedContext), ResourceExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResourceExecuting : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResourceExecuting);
        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutingContext ResourceExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public BeforeOnResourceExecuting(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutingContext = resourceExecutingContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutingContext), ResourceExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResourceExecuting : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterOnResourceExecuting);
        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutingContext ResourceExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public AfterOnResourceExecuting(ActionDescriptor actionDescriptor, ResourceExecutingContext resourceExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutingContext = resourceExecutingContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutingContext), ResourceExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResourceExecuted : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResourceExecuted);
        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutedContext ResourceExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public BeforeOnResourceExecuted(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutedContext = resourceExecutedContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutedContext), ResourceExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResourceExecuted : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterOnResourceExecuted);
        public ActionDescriptor ActionDescriptor { get; }
        public ResourceExecutedContext ResourceExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public AfterOnResourceExecuted(ActionDescriptor actionDescriptor, ResourceExecutedContext resourceExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResourceExecutedContext = resourceExecutedContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResourceExecutedContext), ResourceExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnException : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeOnException);
        public ActionDescriptor ActionDescriptor { get; }
        public ExceptionContext ExceptionContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public BeforeOnException(ActionDescriptor actionDescriptor, ExceptionContext exceptionContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ExceptionContext = exceptionContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ExceptionContext), ExceptionContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnException : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterOnException);
        public ActionDescriptor ActionDescriptor { get; }
        public ExceptionContext ExceptionContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public AfterOnException(ActionDescriptor actionDescriptor, ExceptionContext exceptionContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ExceptionContext = exceptionContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ExceptionContext), ExceptionContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnActionExecution : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeOnActionExecution);
        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutingContext ActionExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public BeforeOnActionExecution(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutingContext = actionExecutingContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutingContext), ActionExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnActionExecution : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterOnActionExecution);
        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutedContext ActionExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public AfterOnActionExecution(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutedContext = actionExecutedContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutedContext), ActionExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnActionExecuting : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeOnActionExecuting);
        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutingContext ActionExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public BeforeOnActionExecuting(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutingContext = actionExecutingContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutingContext), ActionExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnActionExecuting : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterOnActionExecuting);
        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutingContext ActionExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public AfterOnActionExecuting(ActionDescriptor actionDescriptor, ActionExecutingContext actionExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutingContext = actionExecutingContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutingContext), ActionExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnActionExecuted : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeOnActionExecuted);
        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutedContext ActionExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public BeforeOnActionExecuted(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutedContext = actionExecutedContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutedContext), ActionExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnActionExecuted : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterOnActionExecuted);
        public ActionDescriptor ActionDescriptor { get; }
        public ActionExecutedContext ActionExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public AfterOnActionExecuted(ActionDescriptor actionDescriptor, ActionExecutedContext actionExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ActionExecutedContext = actionExecutedContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ActionExecutedContext), ActionExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeActionMethod : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeActionMethod);
        public ActionContext ActionContext { get; }
        public IDictionary<string, object> Arguments { get; }
        public object Controller { get; }

        public override int Count => 3;

        public BeforeActionMethod(ActionContext actionContext, IDictionary<string, object> arguments, object controller)
        {
            ActionContext = actionContext;
            Arguments = arguments;
            Controller = controller;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(Arguments), Arguments),
            2 => new KeyValuePair<string, object>(nameof(Controller), Controller),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterActionMethod : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterActionMethod);
        public ActionContext ActionContext { get; }
        public IDictionary<string, object> Arguments { get; }
        public object Controller { get; }
        public IActionResult Result { get; }

        public override int Count => 4;

        public AfterActionMethod(ActionContext actionContext, IDictionary<string, object> arguments, object controller, IActionResult result)
        {
            ActionContext = actionContext;
            Arguments = arguments;
            Controller = controller;
            Result = result;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(Controller), Controller),
            2 => new KeyValuePair<string, object>(nameof(Controller), Controller),
            3 => new KeyValuePair<string, object>(nameof(Result), Result),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResultExecution : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResultExecution);
        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutingContext ResultExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public BeforeOnResultExecution(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutingContext = resultExecutingContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutingContext), ResultExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResultExecution : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterOnResultExecution);
        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutedContext ResultExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public AfterOnResultExecution(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutedContext = resultExecutedContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutedContext), ResultExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResultExecuting : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResultExecuting);
        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutingContext ResultExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public BeforeOnResultExecuting(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutingContext = resultExecutingContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutingContext), ResultExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResultExecuting : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterOnResultExecuting);
        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutingContext ResultExecutingContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public AfterOnResultExecuting(ActionDescriptor actionDescriptor, ResultExecutingContext resultExecutingContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutingContext = resultExecutingContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutingContext), ResultExecutingContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeOnResultExecuted : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeOnResultExecuted);
        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutedContext ResultExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public BeforeOnResultExecuted(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutedContext = resultExecutedContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutedContext), ResultExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterOnResultExecuted : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterOnResultExecuted);
        public ActionDescriptor ActionDescriptor { get; }
        public ResultExecutedContext ResultExecutedContext { get; }
        public IFilterMetadata Filter { get; }

        public override int Count => 3;

        public AfterOnResultExecuted(ActionDescriptor actionDescriptor, ResultExecutedContext resultExecutedContext, IFilterMetadata filter)
        {
            ActionDescriptor = actionDescriptor;
            ResultExecutedContext = resultExecutedContext;
            Filter = filter;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ResultExecutedContext), ResultExecutedContext),
            2 => new KeyValuePair<string, object>(nameof(Filter), Filter),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeActionResult : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeActionResult);
        public ActionContext ActionContext { get; }
        public IActionResult Result { get; }

        public override int Count => 2;

        public BeforeActionResult(ActionContext actionContext, IActionResult result)
        {
            ActionContext = actionContext;
            Result = result;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(Result), Result),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterActionResult : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterActionResult);
        public ActionContext ActionContext { get; }
        public IActionResult Result { get; }

        public override int Count => 2;

        public AfterActionResult(ActionContext actionContext, IActionResult result)
        {
            ActionContext = actionContext;
            Result = result;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(Result), Result),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }
}