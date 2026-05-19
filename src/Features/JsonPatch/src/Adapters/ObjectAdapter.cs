// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Shared;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Adapters;

/// <inheritdoc />
public class ObjectAdapter : IObjectAdapterWithTest
{
    /// <summary>
    /// Initializes a new instance of <see cref="ObjectAdapter"/>.
    /// </summary>
    /// <param name="contractResolver">The <see cref="IContractResolver"/>.</param>
    /// <param name="logErrorAction">The <see cref="Action"/> for logging <see cref="JsonPatchError"/>.</param>
    public ObjectAdapter(
        IContractResolver contractResolver,
        Action<JsonPatchError> logErrorAction) :
        this(contractResolver, logErrorAction, Adapters.AdapterFactory.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectAdapter"/>.
    /// </summary>
    /// <param name="contractResolver">The <see cref="IContractResolver"/>.</param>
    /// <param name="logErrorAction">The <see cref="Action"/> for logging <see cref="JsonPatchError"/>.</param>
    /// <param name="adapterFactory">The <see cref="IAdapterFactory"/> to use when creating adaptors.</param>
    public ObjectAdapter(
       IContractResolver contractResolver,
       Action<JsonPatchError> logErrorAction,
       IAdapterFactory adapterFactory)
    {
        ContractResolver = contractResolver ?? throw new ArgumentNullException(nameof(contractResolver));
        LogErrorAction = logErrorAction;
        AdapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
    }

    /// <summary>
    /// Gets or sets the <see cref="IContractResolver"/>.
    /// </summary>
    public IContractResolver ContractResolver { get; }

    /// <summary>
    /// Gets or sets the <see cref="IAdapterFactory"/>
    /// </summary>
    public IAdapterFactory AdapterFactory { get; }

    /// <summary>
    /// Action for logging <see cref="JsonPatchError"/>.
    /// </summary>
    public Action<JsonPatchError> LogErrorAction { get; }

    public void Add(Operation operation, object objectToApplyTo)
    {
        ArgumentNullThrowHelper.ThrowIfNull(operation);
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);

        Add(operation.path, operation.value, objectToApplyTo, operation);
    }

    /// <summary>
    /// Add is used by various operations (eg: add, copy, ...), yet through different operations;
    /// This method allows code reuse yet reporting the correct operation on error
    /// </summary>
    private void Add(
        string path,
        object value,
        object objectToApplyTo,
        Operation operation)
    {
        ArgumentNullThrowHelper.ThrowIfNull(path);
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);
        ArgumentNullThrowHelper.ThrowIfNull(operation);

        var parsedPath = new ParsedPath(path);
        var visitor = new ObjectVisitor(parsedPath, ContractResolver, AdapterFactory);

        var target = objectToApplyTo;
        if (!visitor.TryVisit(ref target, out var adapter, out var errorMessage))
        {
            var error = CreatePathNotFoundError(objectToApplyTo, path, operation, errorMessage);
            ErrorReporter(error);
            return;
        }

        if (!adapter.TryAdd(target, parsedPath.LastSegment, ContractResolver, value, out errorMessage))
        {
            var error = CreateOperationFailedError(objectToApplyTo, path, operation, errorMessage);
            ErrorReporter(error);
            return;
        }
    }

    public void Move(Operation operation, object objectToApplyTo)
    {
        ArgumentNullThrowHelper.ThrowIfNull(operation);
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);

        // Get value at 'from' location and add that value to the 'path' location
        if (TryGetValue(operation.from, objectToApplyTo, operation, out var propertyValue))
        {
            // remove that value
            Remove(operation.from, objectToApplyTo, operation);

            // add that value to the path location
            Add(operation.path,
                propertyValue,
                objectToApplyTo,
                operation);
        }
    }

    public void Remove(Operation operation, object objectToApplyTo)
    {
        ArgumentNullThrowHelper.ThrowIfNull(operation);
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);

        Remove(operation.path, objectToApplyTo, operation);
    }

    /// <summary>
    /// Remove is used by various operations (eg: remove, move, ...), yet through different operations;
    /// This method allows code reuse yet reporting the correct operation on error.  The return value
    /// contains the type of the item that has been removed (and a bool possibly signifying an error)
    /// This can be used by other methods, like replace, to ensure that we can pass in the correctly
    /// typed value to whatever method follows.
    /// </summary>
    private void Remove(string path, object objectToApplyTo, Operation operationToReport)
    {
        var parsedPath = new ParsedPath(path);
        var visitor = new ObjectVisitor(parsedPath, ContractResolver, AdapterFactory);

        var target = objectToApplyTo;
        if (!visitor.TryVisit(ref target, out var adapter, out var errorMessage))
        {
            var error = CreatePathNotFoundError(objectToApplyTo, path, operationToReport, errorMessage);
            ErrorReporter(error);
            return;
        }

        if (!adapter.TryRemove(target, parsedPath.LastSegment, ContractResolver, out errorMessage))
        {
            var error = CreateOperationFailedError(objectToApplyTo, path, operationToReport, errorMessage);
            ErrorReporter(error);
            return;
        }
    }

    public void Replace(Operation operation, object objectToApplyTo)
    {
        ArgumentNullThrowHelper.ThrowIfNull(operation);
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);

        var parsedPath = new ParsedPath(operation.path);
        var visitor = new ObjectVisitor(parsedPath, ContractResolver, AdapterFactory);

        var target = objectToApplyTo;
        if (!visitor.TryVisit(ref target, out var adapter, out var errorMessage))
        {
            var error = CreatePathNotFoundError(objectToApplyTo, operation.path, operation, errorMessage);
            ErrorReporter(error);
            return;
        }

        if (!adapter.TryReplace(target, parsedPath.LastSegment, ContractResolver, operation.value, out errorMessage))
        {
            var error = CreateOperationFailedError(objectToApplyTo, operation.path, operation, errorMessage);
            ErrorReporter(error);
            return;
        }
    }

    public void Copy(Operation operation, object objectToApplyTo)
    {
        ArgumentNullThrowHelper.ThrowIfNull(operation);
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);

        // Get value at 'from' location and add that value to the 'path' location
        if (TryGetValue(operation.from, objectToApplyTo, operation, out var propertyValue))
        {
            // Create deep copy
            var copyResult = ConversionResultProvider.CopyTo(propertyValue, propertyValue?.GetType());
            if (copyResult.CanBeConverted)
            {
                Add(operation.path,
                    copyResult.ConvertedInstance,
                    objectToApplyTo,
                    operation);
            }
            else
            {
                var error = CreateOperationFailedError(objectToApplyTo, operation.path, operation, Resources.FormatCannotCopyProperty(operation.from));
                ErrorReporter(error);
                return;
            }
        }
    }

    public void Test(Operation operation, object objectToApplyTo)
    {
        ArgumentNullThrowHelper.ThrowIfNull(operation);
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);

        var parsedPath = new ParsedPath(operation.path);
        var visitor = new ObjectVisitor(parsedPath, ContractResolver, AdapterFactory);

        var target = objectToApplyTo;
        if (!visitor.TryVisit(ref target, out var adapter, out var errorMessage))
        {
            var error = CreatePathNotFoundError(objectToApplyTo, operation.path, operation, errorMessage);
            ErrorReporter(error);
            return;
        }

        if (!adapter.TryTest(target, parsedPath.LastSegment, ContractResolver, operation.value, out errorMessage))
        {
            var error = CreateOperationFailedError(objectToApplyTo, operation.path, operation, errorMessage);
            ErrorReporter(error);
            return;
        }
    }

    private bool TryGetValue(
        string fromLocation,
        object objectToGetValueFrom,
        Operation operation,
        out object propertyValue)
    {
        ArgumentNullThrowHelper.ThrowIfNull(fromLocation);
        ArgumentNullThrowHelper.ThrowIfNull(objectToGetValueFrom);
        ArgumentNullThrowHelper.ThrowIfNull(operation);

        propertyValue = null;

        var parsedPath = new ParsedPath(fromLocation);
        var visitor = new ObjectVisitor(parsedPath, ContractResolver, AdapterFactory);

        var target = objectToGetValueFrom;
        if (!visitor.TryVisit(ref target, out var adapter, out var errorMessage))
        {
            var error = CreatePathNotFoundError(objectToGetValueFrom, fromLocation, operation, errorMessage);
            ErrorReporter(error);
            return false;
        }

        if (!adapter.TryGet(target, parsedPath.LastSegment, ContractResolver, out propertyValue, out errorMessage))
        {
            var error = CreateOperationFailedError(objectToGetValueFrom, fromLocation, operation, errorMessage);
            ErrorReporter(error);
            return false;
        }

        return true;
    }

    private Action<JsonPatchError> ErrorReporter
    {
        get
        {
            return LogErrorAction ?? Internal.ErrorReporter.Default;
        }
    }

    private static JsonPatchError CreateOperationFailedError(object target, string path, Operation operation, string errorMessage)
    {
        return new JsonPatchError(
            target,
            operation,
            errorMessage ?? Resources.FormatCannotPerformOperation(operation.op, path));
    }

    private static JsonPatchError CreatePathNotFoundError(object target, string path, Operation operation, string errorMessage)
    {
        return new JsonPatchError(
            target,
            operation,
            errorMessage ?? Resources.FormatTargetLocationNotFound(operation.op, path));
    }
}
