// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public interface IViewComponentInvokerProvider
    {
        int Order { get; }
        void OnProvidersExecuting([NotNull] ViewComponentInvokerProviderContext context);
        void OnProvidersExecuted([NotNull] ViewComponentInvokerProviderContext context);
    }
}
