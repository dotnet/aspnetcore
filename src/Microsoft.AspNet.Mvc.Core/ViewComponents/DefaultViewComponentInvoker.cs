// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultViewComponentInvoker : IViewComponentInvoker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TypeInfo _componentType;
        private readonly object[] _args;

        public DefaultViewComponentInvoker(
            [NotNull] IServiceProvider serviceProvider, 
            [NotNull] TypeInfo componentType, 
            object[] args)
        {
            _serviceProvider = serviceProvider;
            _componentType = componentType;
            _args = args ?? new object[0];
        }

        public void Invoke([NotNull] ViewComponentContext context)
        {
            var method = ViewComponentMethodSelector.FindSyncMethod(_componentType, _args);
            if (method == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_CannotFindMethod(ViewComponentMethodSelector.SyncMethodName));
            }

            var result = InvokeSyncCore(method, context.ViewContext);
            result.Execute(context);
        }

        public async Task InvokeAsync([NotNull] ViewComponentContext context)
        {
            IViewComponentResult result;

            var asyncMethod = ViewComponentMethodSelector.FindAsyncMethod(_componentType, _args);
            if (asyncMethod == null)
            {
                // We support falling back to synchronous if there is no InvokeAsync method, in this case we'll still
                // execute the IViewResult asynchronously.
                var syncMethod = ViewComponentMethodSelector.FindSyncMethod(_componentType, _args);
                if (syncMethod == null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatViewComponent_CannotFindMethod_WithFallback(
                        ViewComponentMethodSelector.SyncMethodName, ViewComponentMethodSelector.AsyncMethodName));
                }
                else
                {
                    result = InvokeSyncCore(syncMethod, context.ViewContext);
                }
            }
            else
            {
                result = await InvokeAsyncCore(asyncMethod, context.ViewContext);
            }


            await result.ExecuteAsync(context);
        }

        private object CreateComponent([NotNull] ViewContext context)
        {
            var activator = _serviceProvider.GetService<ITypeActivator>();
            object component = activator.CreateInstance(_serviceProvider, _componentType.AsType());

            Injector.InjectProperty(component, "ViewContext", context);

            // We're flowing the viewbag across, but the concept of model doesn't really apply here
            var viewData = new ViewDataDictionary(context.ViewData);
            viewData.Model = null;
            Injector.InjectProperty(component, "ViewData", viewData);

            Injector.CallInitializer(component, _serviceProvider);

            return component;
        }

        private async Task<IViewComponentResult> InvokeAsyncCore([NotNull] MethodInfo method, [NotNull] ViewContext context)
        {
            var component = CreateComponent(context);

            var result = await ReflectedActionExecutor.ExecuteAsync(method, component, _args);

            return CoerceToViewComponentResult(result);
        }

        public IViewComponentResult InvokeSyncCore([NotNull] MethodInfo method, [NotNull] ViewContext context)
        {
            var component = CreateComponent(context);

            object result = null;

            try
            {
                result = method.Invoke(component, _args);
            }
            catch (TargetInvocationException ex)
            {
                // Preserve callstack of any user-thrown exceptions.
                var exceptionInfo = ExceptionDispatchInfo.Capture(ex.InnerException);
                exceptionInfo.Throw();
            }

            return CoerceToViewComponentResult(result);
        }

        private static IViewComponentResult CoerceToViewComponentResult(object value)
        {
            if (value == null)
            {
                throw new InvalidOperationException(Resources.ViewComponent_MustReturnValue);
            }

            var componentResult = value as IViewComponentResult;
            if (componentResult != null)
            {
                return componentResult;
            }

            var stringResult = value as string;
            if (stringResult != null)
            {
                return new ContentViewComponentResult(stringResult);
            }

            var htmlStringResult = value as HtmlString;
            if (htmlStringResult != null)
            {
                return new ContentViewComponentResult(htmlStringResult);
            }

            throw new InvalidOperationException(Resources.FormatViewComponent_InvalidReturnValue(
                typeof(string).Name,
                typeof(HtmlString).Name,
                typeof(IViewComponentResult).Name));
        }
    }
}
