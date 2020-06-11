// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.CompilerServices
{
    /// <summary>
    /// Used by generated code produced by the Components code generator. Not intended or supported
    /// for use in application code.
    /// </summary>
    public static class RuntimeHelpers
    {
        /// <summary>
        /// Not intended for use by application code.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T TypeCheck<T>(T value) => value;

        /// <summary>
        /// Not intended for use by application code.
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="callback"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        //
        // This method is used with `@bind-Value` for components. When a component has a generic type, it's
        // really messy to write to try and write the parameter type for ValueChanged - because it can contain generic
        // type parameters. We're using a trick of type inference to generate the proper typing for the delegate
        // so that method-group-to-delegate conversion works.
        public static EventCallback<T> CreateInferredEventCallback<T>(object receiver, Action<T> callback, T value)
        {
            return EventCallback.Factory.Create<T>(receiver, callback);
        }

        /// <summary>
        /// Not intended for use by application code.
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="callback"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        //
        // This method is used with `@bind-Value` for components. When a component has a generic type, it's
        // really messy to write to try and write the parameter type for ValueChanged - because it can contain generic
        // type parameters. We're using a trick of type inference to generate the proper typing for the delegate
        // so that method-group-to-delegate conversion works.
        public static EventCallback<T> CreateInferredEventCallback<T>(object receiver, Func<T, Task> callback, T value)
        {
            return EventCallback.Factory.Create<T>(receiver, callback);
        }
    }
}
