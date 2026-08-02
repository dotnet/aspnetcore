// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.AspNetCore.Shared;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Adapters;

/// <summary>
/// The default AdapterFactory to be used for resolving <see cref="IAdapter"/>.
/// </summary>
public class AdapterFactory : IAdapterFactory
{
    internal static AdapterFactory Default { get; } = new();

    /// <inheritdoc />
#pragma warning disable PUB0001
    public virtual IAdapter Create(object target, IContractResolver contractResolver)
#pragma warning restore PUB0001
    {
        ArgumentNullThrowHelper.ThrowIfNull(target);
        ArgumentNullThrowHelper.ThrowIfNull(contractResolver);

        var jsonContract = contractResolver.ResolveContract(target.GetType());

        if (target is JObject)
        {
            return new JObjectAdapter();
        }
        if (target is IList)
        {
            return new ListAdapter();
        }
        else if (jsonContract is JsonDictionaryContract jsonDictionaryContract)
        {
            var type = typeof(DictionaryAdapter<,>).MakeGenericType(jsonDictionaryContract.DictionaryKeyType, jsonDictionaryContract.DictionaryValueType);
            return (IAdapter)Activator.CreateInstance(type);
        }
        else if (jsonContract is JsonDynamicContract)
        {
            return new DynamicObjectAdapter();
        }
        else
        {
            return new PocoAdapter();
        }
    }
}

