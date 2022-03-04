// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    }
}
