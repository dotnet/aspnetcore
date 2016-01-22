// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.JsonPatch.Operations;

namespace Microsoft.AspNetCore.JsonPatch.Adapters
{
    /// <summary>
    /// Defines the operations that can be performed on a JSON patch document.
    /// </summary>  
    public interface IObjectAdapter    
    {
        void Add(Operation operation, object objectToApplyTo);
        void Copy(Operation operation, object objectToApplyTo);
        void Move(Operation operation, object objectToApplyTo);
        void Remove(Operation operation, object objectToApplyTo);
        void Replace(Operation operation, object objectToApplyTo);
    }
}