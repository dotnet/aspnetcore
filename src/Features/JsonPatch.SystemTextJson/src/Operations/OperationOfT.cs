// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Adapters;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Exceptions;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

public class Operation<TModel> : Operation where TModel : class
{
    public Operation()
    {
    }

    public Operation(string op, string path, string from, object value)
        : base(op, path, from)
    {
        ArgumentNullThrowHelper.ThrowIfNull(op);
        ArgumentNullThrowHelper.ThrowIfNull(path);

        this.value = value;
    }

    public Operation(string op, string path, string from)
        : base(op, path, from)
    {
        ArgumentNullThrowHelper.ThrowIfNull(op);
        ArgumentNullThrowHelper.ThrowIfNull(path);
    }

    public void Apply(TModel objectToApplyTo, IObjectAdapter adapter)
    {
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);
        ArgumentNullThrowHelper.ThrowIfNull(adapter);

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
                    throw new JsonPatchException(new JsonPatchError(objectToApplyTo, this, Resources.TestOperationNotSupported));
                }
            case OperationType.Invalid:
                throw new JsonPatchException(
                    Resources.FormatInvalidJsonPatchOperation(op), innerException: null);
            default:
                break;
        }
    }
}
