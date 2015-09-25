// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public interface IViewComponentInvoker
    {
        void Invoke(ViewComponentContext context);

        Task InvokeAsync(ViewComponentContext context);
    }
}
