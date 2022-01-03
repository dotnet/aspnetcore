// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

internal static class PropertyValueSetter
{
    private static readonly MethodInfo CallPropertyAddRangeOpenGenericMethod =
        typeof(PropertyValueSetter).GetMethod(nameof(CallPropertyAddRange), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static void SetValue(
        ModelMetadata metadata,
        object instance,
        object? value)
    {
        if (!metadata.IsReadOnly)
        {
            // Handle settable property. Do not set the property to null if the type is a non-nullable type.
            if (value != null || metadata.IsReferenceOrNullableType)
            {
                metadata.PropertySetter!(instance, value);
            }

            return;
        }

        if (metadata.ModelType.IsArray)
        {
            // Do not attempt to copy values into an array because an array's length is immutable. This choice
            // is also consistent with ComplexTypeModelBinder's handling of a read-only array property.
            return;
        }

        if (!metadata.IsCollectionType)
        {
            // Not a collection model.
            return;
        }

        var target = metadata.PropertyGetter!(instance);
        if (value == null || target == null)
        {
            // Nothing to do when source or target is null.
            return;
        }

        // Handle a read-only collection property.
        var propertyAddRange = CallPropertyAddRangeOpenGenericMethod.MakeGenericMethod(
            metadata.ElementMetadata!.ModelType);
        propertyAddRange.Invoke(obj: null, parameters: new[] { target, value });
    }

    // Called via reflection.
    private static void CallPropertyAddRange<TElement>(object target, object source)
    {
        var targetCollection = (ICollection<TElement>)target;
        if (source is IEnumerable<TElement> sourceCollection && !targetCollection.IsReadOnly)
        {
            targetCollection.Clear();
            foreach (var item in sourceCollection)
            {
                targetCollection.Add(item);
            }
        }
    }
}
