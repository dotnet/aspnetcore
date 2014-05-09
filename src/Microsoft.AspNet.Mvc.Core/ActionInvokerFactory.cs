// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerFactory : IActionInvokerFactory
    {
        private readonly INestedProviderManager<ActionInvokerProviderContext> _actionInvokerProvider;

        public ActionInvokerFactory(INestedProviderManager<ActionInvokerProviderContext> actionInvokerProvider)
        {
            _actionInvokerProvider = actionInvokerProvider;
        }

        public IActionInvoker CreateInvoker(ActionContext actionContext)
        {
            var context = new ActionInvokerProviderContext(actionContext);
            _actionInvokerProvider.Invoke(context);
            return context.Result;
        }
    }
}
