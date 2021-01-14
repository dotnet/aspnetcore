// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.JsonPatch.Helpers
{
    /// <summary>
    /// Return value for the helper method used by Copy/Move.  Needed to ensure we can make a different
    /// decision in the calling method when the value is null because it cannot be fetched (HasError = true) 
    /// versus when it actually is null (much like why RemovedPropertyTypeResult is used for returning 
    /// type in the Remove operation).
    /// </summary>
    public class GetValueResult
    {
        public GetValueResult(object propertyValue, bool hasError)
        {
            PropertyValue = propertyValue;
            HasError = hasError;
        }

        /// <summary>
        /// The value of the property we're trying to get
        /// </summary>
        public object PropertyValue { get; private set; }

        /// <summary>
        /// HasError: true when an error occurred, the operation didn't complete successfully
        /// </summary>
        public bool HasError { get; private set; }
    }
}
