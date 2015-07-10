// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.JsonPatch.Adapters;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.JsonPatch.Operations
{
    public class Operation<TModel> : Operation where TModel : class
    {
        public Operation()
        {

        }

        public Operation([NotNull] string op, [NotNull] string path, string from, object value)
            : base(op, path, from)
        {
            this.value = value;
        }

        public Operation([NotNull] string op, [NotNull] string path, string from)
            : base(op, path, from)
        {

        }

        public void Apply([NotNull] TModel objectToApplyTo, [NotNull] IObjectAdapter adapter)
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

    }
}