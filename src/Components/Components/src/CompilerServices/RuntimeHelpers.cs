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
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <remarks>
        /// The method is designed to be used in the logic that generates the
        /// runtime code for a given delegate attribute in the compiler.
        /// Components accept delegate attributes with values that include:
        /// * References to Action parameters
        /// * References to a method group
        /// * Actions defined via a lambda expression
        /// Previously, the Razor compiler used a {node.TypeName} constructor to
        /// support converting method groups to a delegate types in the compiler.
        /// However, this made it impossible to pass nullable Action types to
        /// components. Instead of using the constructor to support the conversion,
        /// we use the `TypeCheckDelegate` conversion which handles the method group
        /// conversion and supports null values of T.
        /// </remarks>
        public static T TypeCheckDelegate<T>(T value)
        {
            T explicitValue = (T) value;
            return explicitValue;
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
