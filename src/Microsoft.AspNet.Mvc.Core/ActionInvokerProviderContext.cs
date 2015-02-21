// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerProviderContext
    {
        public ActionInvokerProviderContext([NotNull]ActionContext actionContext)
        {
            ActionContext = actionContext;
        }

        public ActionContext ActionContext { get; private set; }

        public IActionInvoker Result { get; set; }
    }
}
