// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Adapters;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;

internal class ObjectVisitor
{
    private readonly IAdapterFactory _adapterFactory;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ParsedPath _path;

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectVisitor"/>.
    /// </summary>
    /// <param name="path">The path of the JsonPatch operation</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
    public ObjectVisitor(ParsedPath path, JsonSerializerOptions serializerOptions)
        : this(path, serializerOptions, AdapterFactory.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectVisitor"/>.
    /// </summary>
    /// <param name="path">The path of the JsonPatch operation</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="adapterFactory">The <see cref="IAdapterFactory"/> to use when creating adaptors.</param>
    public ObjectVisitor(ParsedPath path, JsonSerializerOptions serializerOptions, IAdapterFactory adapterFactory)
    {
        _path = path;
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
    }

    public bool TryVisit(ref object target, out IAdapter adapter, out string errorMessage)
    {
        if (target == null)
        {
            adapter = null;
            errorMessage = null;
            return false;
        }

        adapter = SelectAdapter(target);

        // Traverse until the penultimate segment to get the target object and adapter
        for (var i = 0; i < _path.Segments.Count - 1; i++)
        {
            if (!adapter.TryTraverse(target, _path.Segments[i], _serializerOptions, out var next, out errorMessage))
            {
                adapter = null;
                return false;
            }

            // If we hit a null on an interior segment then we need to stop traversing.
            if (next == null)
            {
                adapter = null;
                return false;
            }

            target = next;
            adapter = SelectAdapter(target);
        }

        errorMessage = null;
        return true;
    }

    private IAdapter SelectAdapter(object targetObject)
    {
        return _adapterFactory.Create(targetObject);
    }
}
