// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop.Internal
{
    // This is "soft" internal because we're trying to avoid expanding JsonUtil into a sophisticated
    // API. Developers who want that would be better served by using a different JSON package
    // instead. Also the perf implications of the ICustomArgSerializer approach aren't ideal
    // (it forces structs to be boxed, and returning a dictionary means lots more allocations
    // and boxing of any value-typed properties).

    /// <summary>
    /// Internal. Intended for framework use only.
    /// </summary>
    public interface ICustomArgSerializer
    {
        /// <summary>
        /// Internal. Intended for framework use only.
        /// </summary>
        object ToJsonPrimitive();
    }
}
