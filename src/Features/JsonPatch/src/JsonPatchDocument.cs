// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Converters;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch;

// Implementation details: the purpose of this type of patch document is to allow creation of such
// documents for cases where there's no class/DTO to work on. Typical use case: backend not built in
// .NET or architecture doesn't contain a shared DTO layer.
[JsonConverter(typeof(JsonPatchDocumentConverter))]
public class JsonPatchDocument : IJsonPatchDocument
{
    public List<Operation> Operations { get; private set; }

    [JsonIgnore]
    public IContractResolver ContractResolver { get; set; }

    public JsonPatchDocument()
    {
        Operations = new List<Operation>();
        ContractResolver = new DefaultContractResolver();
    }

    public JsonPatchDocument(List<Operation> operations, IContractResolver contractResolver)
    {
        ArgumentNullThrowHelper.ThrowIfNull(operations);
        ArgumentNullThrowHelper.ThrowIfNull(contractResolver);

        Operations = operations;
        ContractResolver = contractResolver;
    }

    /// <summary>
    /// Add operation.  Will result in, for example,
    /// { "op": "add", "path": "/a/b/c", "value": [ "foo", "bar" ] }
    /// </summary>
    /// <param name="path">target location</param>
    /// <param name="value">value</param>
    /// <returns>The <see cref="JsonPatchDocument"/> for chaining.</returns>
    public JsonPatchDocument Add(string path, object value)
    {
        ArgumentNullThrowHelper.ThrowIfNull(path);

        Operations.Add(new Operation("add", PathHelpers.ValidateAndNormalizePath(path), null, value));
        return this;
    }

    /// <summary>
    /// Remove value at target location.  Will result in, for example,
    /// { "op": "remove", "path": "/a/b/c" }
    /// </summary>
    /// <param name="path">target location</param>
    /// <returns>The <see cref="JsonPatchDocument"/> for chaining.</returns>
    public JsonPatchDocument Remove(string path)
    {
        ArgumentNullThrowHelper.ThrowIfNull(path);

        Operations.Add(new Operation("remove", PathHelpers.ValidateAndNormalizePath(path), null, null));
        return this;
    }

    /// <summary>
    /// Replace value.  Will result in, for example,
    /// { "op": "replace", "path": "/a/b/c", "value": 42 }
    /// </summary>
    /// <param name="path">target location</param>
    /// <param name="value">value</param>
    /// <returns>The <see cref="JsonPatchDocument"/> for chaining.</returns>
    public JsonPatchDocument Replace(string path, object value)
    {
        ArgumentNullThrowHelper.ThrowIfNull(path);

        Operations.Add(new Operation("replace", PathHelpers.ValidateAndNormalizePath(path), null, value));
        return this;
    }

    /// <summary>
    /// Test value.  Will result in, for example,
    /// { "op": "test", "path": "/a/b/c", "value": 42 }
    /// </summary>
    /// <param name="path">target location</param>
    /// <param name="value">value</param>
    /// <returns>The <see cref="JsonPatchDocument"/> for chaining.</returns>
    public JsonPatchDocument Test(string path, object value)
    {
        ArgumentNullThrowHelper.ThrowIfNull(path);

        Operations.Add(new Operation("test", PathHelpers.ValidateAndNormalizePath(path), null, value));
        return this;
    }

    /// <summary>
    /// Removes value at specified location and add it to the target location.  Will result in, for example:
    /// { "op": "move", "from": "/a/b/c", "path": "/a/b/d" }
    /// </summary>
    /// <param name="from">source location</param>
    /// <param name="path">target location</param>
    /// <returns>The <see cref="JsonPatchDocument"/> for chaining.</returns>
    public JsonPatchDocument Move(string from, string path)
    {
        ArgumentNullThrowHelper.ThrowIfNull(from);
        ArgumentNullThrowHelper.ThrowIfNull(path);

        Operations.Add(new Operation("move", PathHelpers.ValidateAndNormalizePath(path), PathHelpers.ValidateAndNormalizePath(from)));
        return this;
    }

    /// <summary>
    /// Copy the value at specified location to the target location.  Will result in, for example:
    /// { "op": "copy", "from": "/a/b/c", "path": "/a/b/e" }
    /// </summary>
    /// <param name="from">source location</param>
    /// <param name="path">target location</param>
    /// <returns>The <see cref="JsonPatchDocument"/> for chaining.</returns>
    public JsonPatchDocument Copy(string from, string path)
    {
        ArgumentNullThrowHelper.ThrowIfNull(from);
        ArgumentNullThrowHelper.ThrowIfNull(path);

        Operations.Add(new Operation("copy", PathHelpers.ValidateAndNormalizePath(path), PathHelpers.ValidateAndNormalizePath(from)));
        return this;
    }

    /// <summary>
    /// Apply this JsonPatchDocument
    /// </summary>
    /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
    public void ApplyTo(object objectToApplyTo)
    {
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);

        ApplyTo(objectToApplyTo, new ObjectAdapter(ContractResolver, null, AdapterFactory.Default));
    }

    /// <summary>
    /// Apply this JsonPatchDocument
    /// </summary>
    /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
    /// <param name="logErrorAction">Action to log errors</param>
    public void ApplyTo(object objectToApplyTo, Action<JsonPatchError> logErrorAction)
    {
        ApplyTo(objectToApplyTo, new ObjectAdapter(ContractResolver, logErrorAction, AdapterFactory.Default), logErrorAction);
    }

    /// <summary>
    /// Apply this JsonPatchDocument
    /// </summary>
    /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
    /// <param name="adapter">IObjectAdapter instance to use when applying</param>
    /// <param name="logErrorAction">Action to log errors</param>
    public void ApplyTo(object objectToApplyTo, IObjectAdapter adapter, Action<JsonPatchError> logErrorAction)
    {
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);
        ArgumentNullThrowHelper.ThrowIfNull(adapter);

        foreach (var op in Operations)
        {
            try
            {
                op.Apply(objectToApplyTo, adapter);
            }
            catch (JsonPatchException jsonPatchException)
            {
                var errorReporter = logErrorAction ?? ErrorReporter.Default;
                errorReporter(new JsonPatchError(objectToApplyTo, op, jsonPatchException.Message));

                // As per JSON Patch spec if an operation results in error, further operations should not be executed.
                break;
            }
        }
    }

    /// <summary>
    /// Apply this JsonPatchDocument
    /// </summary>
    /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
    /// <param name="adapter">IObjectAdapter instance to use when applying</param>
    public void ApplyTo(object objectToApplyTo, IObjectAdapter adapter)
    {
        ArgumentNullThrowHelper.ThrowIfNull(objectToApplyTo);
        ArgumentNullThrowHelper.ThrowIfNull(adapter);

        // apply each operation in order
        foreach (var op in Operations)
        {
            op.Apply(objectToApplyTo, adapter);
        }
    }

    IList<Operation> IJsonPatchDocument.GetOperations()
    {
        var allOps = new List<Operation>(Operations?.Count ?? 0);

        if (Operations != null)
        {
            foreach (var op in Operations)
            {
                var untypedOp = new Operation();

                untypedOp.op = op.op;
                untypedOp.value = op.value;
                untypedOp.path = op.path;
                untypedOp.from = op.from;

                allOps.Add(untypedOp);
            }
        }

        return allOps;
    }
}
