using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class ControllerRequestDelegateFactory : IRequestDelegateFactory
    {
        private readonly ControllerActionInvokerCache _controllerActionInvokerCache;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
        private readonly int _maxModelValidationErrors;
        private readonly ILogger _logger;
        private readonly DiagnosticListener _diagnosticListener;
        private readonly IActionResultTypeMapper _mapper;
        private readonly IActionContextAccessor _actionContextAccessor;

        public ControllerRequestDelegateFactory(
            ControllerActionInvokerCache controllerActionInvokerCache,
            IOptions<MvcOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            DiagnosticListener diagnosticListener,
            IActionResultTypeMapper mapper)
            : this(controllerActionInvokerCache, optionsAccessor, loggerFactory, diagnosticListener, mapper, null)
        {
        }

        public ControllerRequestDelegateFactory(
            ControllerActionInvokerCache controllerActionInvokerCache,
            IOptions<MvcOptions> optionsAccessor,
            ILoggerFactory loggerFactory,
            DiagnosticListener diagnosticListener,
            IActionResultTypeMapper mapper,
            IActionContextAccessor actionContextAccessor)
        {
            _controllerActionInvokerCache = controllerActionInvokerCache;
            _valueProviderFactories = optionsAccessor.Value.ValueProviderFactories.ToArray();
            _maxModelValidationErrors = optionsAccessor.Value.MaxModelValidationErrors;
            _logger = loggerFactory.CreateLogger<ControllerActionInvoker>();
            _diagnosticListener = diagnosticListener;
            _mapper = mapper;
            _actionContextAccessor = actionContextAccessor ?? ActionContextAccessor.Null;
        }

        public RequestDelegate CreateRequestDelegate(ActionDescriptor actionDescriptor, RouteValueDictionary dataTokens)
        {
            if (actionDescriptor is ControllerActionDescriptor)
            {
                return context =>
                {
                    RouteData routeData = null;

                    if (dataTokens is null or { Count: 0 })
                    {
                        routeData = new RouteData(context.Request.RouteValues);
                    }
                    else
                    {
                        routeData = new RouteData();
                        routeData.PushState(router: null, context.Request.RouteValues, dataTokens);
                    }

                    var actionContext = new ActionContext(context, routeData, actionDescriptor);

                    var controllerContext = new ControllerContext(actionContext)
                    {
                        // PERF: These are rarely going to be changed, so let's go copy-on-write.
                        ValueProviderFactories = new CopyOnWriteList<IValueProviderFactory>(_valueProviderFactories)
                    };

                    controllerContext.ModelState.MaxAllowedErrors = _maxModelValidationErrors;

                    var (cacheEntry, filters) = _controllerActionInvokerCache.GetCachedResult(controllerContext);

                    var invoker = new ControllerActionInvoker(
                        _logger,
                        _diagnosticListener,
                        _actionContextAccessor,
                        _mapper,
                        controllerContext,
                        cacheEntry,
                        filters);

                    return invoker.InvokeAsync();
                };
            }

            return null;
        }
    }
}
