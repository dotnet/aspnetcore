// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.Json
{
    // This is internal because we're trying to avoid expanding JsonUtil into a sophisticated
    // API. Developers who want that would be better served by using a different JSON package
    // instead. Also the perf implications of the ICustomJsonSerializer approach aren't ideal
    // (it forces structs to be boxed, and returning a dictionary means lots more allocations
    // and boxing of any value-typed properties).
    internal interface ICustomJsonSerializer
    {
        /// <summary>
        /// Supplies a representation suitable for JSON serialization. For example, the
        /// return value may be a string->object dictionary containing properties to
        /// serialize, or simply a string.
        /// </summary>
        /// <returns></returns>
        object ToJsonPrimitive();
    }
}
