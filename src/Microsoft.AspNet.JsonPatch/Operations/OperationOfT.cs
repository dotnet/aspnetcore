// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.JsonPatch.Adapters;

namespace Microsoft.AspNet.JsonPatch.Operations
{
    public class Operation<TModel> : Operation where TModel : class
    {
        public Operation()
        {

        }

        public Operation(string op, string path, string from, object value)
            : base(op, path, from)
        {
            this.value = value;
        }

        public Operation(string op, string path, string from)
            : base(op, path, from)
        {

        }

        public void Apply(TModel objectToApplyTo, IObjectAdapter<TModel> adapter)
        {
            switch (OperationType)
            {
                case OperationType.Add:
                    adapter.Add(this, objectToApplyTo);
                    break;
                case OperationType.Remove:
                    adapter.Remove(this, objectToApplyTo);
                    break;
                case OperationType.Replace:
                    adapter.Replace(this, objectToApplyTo);
                    break;
                case OperationType.Move:
                    adapter.Move(this, objectToApplyTo);
                    break;
                case OperationType.Copy:
                    adapter.Copy(this, objectToApplyTo);
                    break;
                case OperationType.Test:
                    throw new NotSupportedException(Resources.TestOperationNotSupported);
                default:
                    break;
            }
        }

        public bool ShouldSerializevalue()
        {
            return (OperationType == Operations.OperationType.Add
                || OperationType == OperationType.Replace
                || OperationType == OperationType.Test);
        }
    }
}