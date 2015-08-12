// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.JsonPatch.Helpers
{
    /// <summary>
    /// Return value for Remove operation.  The combination tells us what to do next (if this operation
    /// is called from inside another operation, eg: Replace, Copy.
    /// 
    /// Possible combo:
    /// - ActualType contains type: operation succesfully completed, can continue when called from inside
    ///   another operation
    /// - ActualType null and HasError true: operation not completed succesfully, should not be allowed to continue
    /// - ActualType null and HasError false: operation completed succesfully, but we should not be allowed to 
    ///   continue when called from inside another method as we could not verify the type of the removed property.
    ///   This happens when the value of an item in an ExpandoObject dictionary is null.
    /// </summary>
    internal class RemovedPropertyTypeResult
    {
        /// <summary>
        /// The type of the removed property (value)
        /// </summary>
        public Type ActualType { get; private set; }

        /// <summary>
        /// HasError: true when an error occurred, the operation didn't complete succesfully
        /// </summary>
        public bool HasError { get; set; }

        public RemovedPropertyTypeResult(Type actualType, bool hasError)
        {
            ActualType = actualType;
            HasError = hasError;
        }
    }
}
