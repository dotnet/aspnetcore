// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.JsonPatch.Operations;

namespace Microsoft.AspNetCore.JsonPatch.Adapters
{
    /// <summary>
    /// Defines the operations that can be performed on a JSON patch document, including "test".
    /// </summary>
    public interface IObjectAdapterWithTest : IObjectAdapter
    {
        /// <summary>
        /// Using the "test" operation a value at the target location is compared for
        /// equality to a specified value.
        /// 
        /// The operation object MUST contain a "value" member that specifies 
        /// value to be compared to the target location's value.
        /// 
        /// The target location MUST be equal to the "value" value for the 
        /// operation to be considered successful.
        /// 
        /// For example:
        /// { "op": "test", "path": "/a/b/c", "value": "foo" }
        /// 
        /// See RFC 6902 https://tools.ietf.org/html/rfc6902#page-7
        /// </summary>
        /// <param name="operation">The test operation.</param>
        /// <param name="objectToApplyTo">Object to apply the operation to.</param>
        void Test(Operation operation, object objectToApplyTo);
    }
}
