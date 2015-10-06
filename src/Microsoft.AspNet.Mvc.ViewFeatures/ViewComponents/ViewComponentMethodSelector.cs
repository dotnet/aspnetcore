// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ViewFeatures;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public static class ViewComponentMethodSelector
    {
        public const string AsyncMethodName = "InvokeAsync";
        public const string SyncMethodName = "Invoke";

        public static MethodInfo FindAsyncMethod(TypeInfo componentType, object[] args)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var method = GetMethod(componentType, args, AsyncMethodName);
            if (method == null)
            {
                return null;
            }

            if (!method.ReturnType.GetTypeInfo().IsGenericType ||
                method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_AsyncMethod_ShouldReturnTask(AsyncMethodName));
            }

            return method;
        }

        public static MethodInfo FindSyncMethod(TypeInfo componentType, object[] args)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var method = GetMethod(componentType, args, SyncMethodName);
            if (method == null)
            {
                return null;
            }

            if (method.ReturnType == typeof(void))
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_SyncMethod_ShouldReturnValue(SyncMethodName));
            }
            else if (method.ReturnType.IsAssignableFrom(typeof(Task)))
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_SyncMethod_CannotReturnTask(SyncMethodName, nameof(Task)));
            }

            return method;
        }

        private static MethodInfo GetMethod(TypeInfo componentType, object[] args, string methodName)
        {
            Type[] types;
            if (args == null || args.Length == 0)
            {
                types = Type.EmptyTypes;
            }
            else
            {
                types = new Type[args.Length];
                for (var i = 0; i < args.Length; i++)
                {
                    types[i] = args[i]?.GetType() ?? typeof(object);
                }
            }

#if DNX451
            return componentType.AsType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: types,
                modifiers: null);
#else
            var method = componentType.AsType().GetMethod(methodName, types: types);
            // At most one method (including static and instance methods) with the same parameter types can exist
            // per type.
            return method != null && method.IsStatic ? null : method;
#endif
        }
    }
}
