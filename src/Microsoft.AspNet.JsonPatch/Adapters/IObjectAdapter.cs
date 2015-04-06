// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.JsonPatch.Operations;

namespace Microsoft.AspNet.JsonPatch.Adapters
{
    /// <summary>
    /// Defines the operations that can be performed on a JSON patch document.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IObjectAdapter<T> where T : class
    {
        void Add(Operation<T> operation, T objectToApplyTo);
        void Copy(Operation<T> operation, T objectToApplyTo);
        void Move(Operation<T> operation, T objectToApplyTo);
        void Remove(Operation<T> operation, T objectToApplyTo);
        void Replace(Operation<T> operation, T objectToApplyTo);
        void Test(Operation<T> operation, T objectToApplyTo);
    }
}