// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal sealed class MvcEndpointInvokerFactory : IActionInvokerFactory
    {
        private readonly IActionInvokerFactory _invokerFactory;
        private readonly IActionContextAccessor _actionContextAccessor;

        public MvcEndpointInvokerFactory(
            IActionInvokerFactory invokerFactory)
            : this(invokerFactory, actionContextAccessor: null)
        {
        }

        public MvcEndpointInvokerFactory(
            IActionInvokerFactory invokerFactory,
            IActionContextAccessor actionContextAccessor)
        {
            _invokerFactory = invokerFactory;

            // The IActionContextAccessor is optional. We want to avoid the overhead of using CallContext
            // if possible.
            _actionContextAccessor = actionContextAccessor;
        }

        public IActionInvoker CreateInvoker(ActionContext actionContext)
        {
            if (_actionContextAccessor != null)
            {
                _actionContextAccessor.ActionContext = actionContext;
            }

            return _invokerFactory.CreateInvoker(actionContext);
        }
    }
}
