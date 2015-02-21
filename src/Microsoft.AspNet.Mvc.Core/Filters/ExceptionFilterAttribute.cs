// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ExceptionFilterAttribute : Attribute, IAsyncExceptionFilter, IExceptionFilter, IOrderedFilter
    {
        public int Order { get; set; }

#pragma warning disable 1998
        public virtual async Task OnExceptionAsync([NotNull] ExceptionContext context)
        {
            OnException(context);
        }
#pragma warning restore 1998

        public virtual void OnException([NotNull] ExceptionContext context)
        {
        }
    }
}
