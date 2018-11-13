// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.JsonPatch.Operations
{
    public class Operation : OperationBase
    {
        [JsonProperty("value")]
        public object value { get; set; }

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

        public void Apply(object objectToApplyTo, IObjectAdapter adapter)
        {
            if (objectToApplyTo == null)
            {
                throw new ArgumentNullException(nameof(objectToApplyTo));
            }

            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

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
                    if (adapter is IObjectAdapterWithTest adapterWithTest)
                    {
                        adapterWithTest.Test(this, objectToApplyTo);
                        break;
                    }
                    else
                    {
                        throw new NotSupportedException(Resources.TestOperationNotSupported);
                    }
                default:
                    break;
            }
        }

        public bool ShouldSerializevalue()
        {
            return (OperationType == OperationType.Add
                || OperationType == OperationType.Replace
                || OperationType == OperationType.Test);
        }
    }
}