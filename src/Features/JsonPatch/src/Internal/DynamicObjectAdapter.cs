// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using CSharpBinder = Microsoft.CSharp.RuntimeBinder;

namespace Microsoft.AspNetCore.JsonPatch.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public class DynamicObjectAdapter : IAdapter
{
    public virtual bool TryAdd(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        if (!TrySetDynamicObjectProperty(target, contractResolver, segment, value, out errorMessage))
        {
            return false;
        }

        errorMessage = null;
        return true;
    }

    public virtual bool TryGet(
        object target,
        string segment,
        IContractResolver contractResolver,
        out object value,
        out string errorMessage)
    {
        if (!TryGetDynamicObjectProperty(target, contractResolver, segment, out value, out errorMessage))
        {
            value = null;
            return false;
        }

        errorMessage = null;
        return true;
    }

    public virtual bool TryRemove(
        object target,
        string segment,
        IContractResolver contractResolver,
        out string errorMessage)
    {
        if (!TryGetDynamicObjectProperty(target, contractResolver, segment, out var property, out errorMessage))
        {
            return false;
        }

        // Setting the value to "null" will use the default value in case of value types, and
        // null in case of reference types
        object value = null;
        if (property.GetType().IsValueType
            && Nullable.GetUnderlyingType(property.GetType()) == null)
        {
            value = Activator.CreateInstance(property.GetType());
        }

        if (!TrySetDynamicObjectProperty(target, contractResolver, segment, value, out errorMessage))
        {
            return false;
        }

        errorMessage = null;
        return true;
    }

    public virtual bool TryReplace(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        if (!TryGetDynamicObjectProperty(target, contractResolver, segment, out var property, out errorMessage))
        {
            return false;
        }

        if (!TryConvertValue(value, property.GetType(), out var convertedValue))
        {
            errorMessage = Resources.FormatInvalidValueForProperty(value);
            return false;
        }

        if (!TryRemove(target, segment, contractResolver, out errorMessage))
        {
            return false;
        }

        if (!TrySetDynamicObjectProperty(target, contractResolver, segment, convertedValue, out errorMessage))
        {
            return false;
        }

        errorMessage = null;
        return true;
    }

    public virtual bool TryTest(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        if (!TryGetDynamicObjectProperty(target, contractResolver, segment, out var property, out errorMessage))
        {
            return false;
        }

        if (!TryConvertValue(value, property.GetType(), out var convertedValue))
        {
            errorMessage = Resources.FormatInvalidValueForProperty(value);
            return false;
        }

        if (!JToken.DeepEquals(JsonConvert.SerializeObject(property), JsonConvert.SerializeObject(convertedValue)))
        {
            errorMessage = Resources.FormatValueNotEqualToTestValue(property, value, segment);
            return false;
        }
        else
        {
            errorMessage = null;
            return true;
        }
    }

    public virtual bool TryTraverse(
        object target,
        string segment,
        IContractResolver contractResolver,
        out object nextTarget,
        out string errorMessage)
    {
        if (!TryGetDynamicObjectProperty(target, contractResolver, segment, out var property, out errorMessage))
        {
            nextTarget = null;
            return false;
        }
        else
        {
            nextTarget = property;
            errorMessage = null;
            return true;
        }
    }

    protected virtual bool TryGetDynamicObjectProperty(
        object target,
        IContractResolver contractResolver,
        string segment,
        out object value,
        out string errorMessage)
    {
        var jsonDynamicContract = (JsonDynamicContract)contractResolver.ResolveContract(target.GetType());

        var propertyName = jsonDynamicContract.PropertyNameResolver(segment);

        var binder = CSharpBinder.Binder.GetMember(
            CSharpBinderFlags.None,
            propertyName,
            target.GetType(),
            new List<CSharpArgumentInfo>
            {
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
            });

        var callsite = CallSite<Func<CallSite, object, object>>.Create(binder);

        try
        {
            value = callsite.Target(callsite, target);
            errorMessage = null;
            return true;
        }
        catch (RuntimeBinderException)
        {
            value = null;
            errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
            return false;
        }
    }

    protected virtual bool TrySetDynamicObjectProperty(
        object target,
        IContractResolver contractResolver,
        string segment,
        object value,
        out string errorMessage)
    {
        var jsonDynamicContract = (JsonDynamicContract)contractResolver.ResolveContract(target.GetType());

        var propertyName = jsonDynamicContract.PropertyNameResolver(segment);

        var binder = CSharpBinder.Binder.SetMember(
            CSharpBinderFlags.None,
            propertyName,
            target.GetType(),
            new List<CSharpArgumentInfo>
            {
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
            });

        var callsite = CallSite<Func<CallSite, object, object, object>>.Create(binder);

        try
        {
            callsite.Target(callsite, target, value);
            errorMessage = null;
            return true;
        }
        catch (RuntimeBinderException)
        {
            errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
            return false;
        }
    }

    protected virtual bool TryConvertValue(object value, Type propertyType, out object convertedValue)
    {
        var conversionResult = ConversionResultProvider.ConvertTo(value, propertyType);
        if (!conversionResult.CanBeConverted)
        {
            convertedValue = null;
            return false;
        }

        convertedValue = conversionResult.ConvertedInstance;
        return true;
    }
}
