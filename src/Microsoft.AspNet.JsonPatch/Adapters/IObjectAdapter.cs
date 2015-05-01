// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.JsonPatch.Operations;

namespace Microsoft.AspNet.JsonPatch.Adapters
{
    /// <summary>
    /// Defines the operations that can be performed on a JSON patch document.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    public interface IObjectAdapter<TModel> where TModel : class
    {
        void Add(Operation<TModel> operation, TModel objectToApplyTo);
        void Copy(Operation<TModel> operation, TModel objectToApplyTo);
        void Move(Operation<TModel> operation, TModel objectToApplyTo);
        void Remove(Operation<TModel> operation, TModel objectToApplyTo);
        void Replace(Operation<TModel> operation, TModel objectToApplyTo);
    }
}