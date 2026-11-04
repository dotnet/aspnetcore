// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Adapters;

/// <summary>
/// The default AdapterFactory to be used for resolving <see cref="IAdapter"/>.
/// </summary>
internal class AdapterFactory : IAdapterFactory
{
    internal static AdapterFactory Default { get; } = new();

    /// <inheritdoc />
    public virtual IAdapter Create(object target)
    {
        ArgumentNullThrowHelper.ThrowIfNull(target);

        var typeToConvert = target.GetType();
        if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return (IAdapter)Activator.CreateInstance(typeof(DictionaryAdapter<,>).MakeGenericType(typeToConvert.GenericTypeArguments[0], typeToConvert.GenericTypeArguments[1]));
        }

        return target switch
        {
            JsonObject => new JsonObjectAdapter(),
            JsonArray => new ListAdapter(),
            IList => new ListAdapter(),
            _ => new PocoAdapter()
        };
    }
}

