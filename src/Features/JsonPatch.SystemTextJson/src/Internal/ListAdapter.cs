// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Helpers;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;

internal class ListAdapter : IAdapter
{
    public virtual bool TryAdd(object target, string segment, JsonSerializerOptions serializerOptions, object value, out string errorMessage)
    {
        if (!TryGetListTypeArgument(target, out var typeArgument, out errorMessage))
        {
            return false;
        }

        var targetCollectionCount = GenericListOrJsonArrayUtilities.GetCount(target);
        if (!TryGetPositionInfo(targetCollectionCount, segment, OperationType.Add, out var positionInfo, out errorMessage))
        {
            return false;
        }

        if (!TryConvertValue(value, typeArgument, segment, serializerOptions, out var convertedValue, out errorMessage))
        {
            return false;
        }

        if (positionInfo.Type == PositionType.EndOfList)
        {
            GenericListOrJsonArrayUtilities.AddElement(target, convertedValue);
        }
        else
        {
            GenericListOrJsonArrayUtilities.InsertElementAt(target, positionInfo.Index, convertedValue);
        }

        errorMessage = null;
        return true;
    }

    public virtual bool TryGet(object target, string segment, JsonSerializerOptions serializerOptions, out object value, out string errorMessage)
    {
        if (!TryGetListTypeArgument(target, out _, out errorMessage))
        {
            value = null;
            return false;
        }

        var targetCollectionCount = GenericListOrJsonArrayUtilities.GetCount(target);

        if (!TryGetPositionInfo(targetCollectionCount, segment, OperationType.Get, out var positionInfo, out errorMessage))
        {
            value = null;
            return false;
        }

        var valueIndex = positionInfo.Type == PositionType.EndOfList ? targetCollectionCount - 1 : positionInfo.Index;
        value = GenericListOrJsonArrayUtilities.GetElementAt(target, valueIndex);

        errorMessage = null;
        return true;
    }

    public virtual bool TryRemove(object target, string segment, JsonSerializerOptions serializerOptions, out string errorMessage)
    {
        if (!TryGetListTypeArgument(target, out _, out errorMessage))
        {
            return false;
        }

        var count = GenericListOrJsonArrayUtilities.GetCount(target);
        if (!TryGetPositionInfo(count, segment, OperationType.Remove, out var positionInfo, out errorMessage))
        {
            return false;
        }

        var indexToRemove = positionInfo.Type == PositionType.EndOfList ? count - 1 : positionInfo.Index;
        GenericListOrJsonArrayUtilities.RemoveElementAt(target, indexToRemove);

        errorMessage = null;
        return true;
    }

    public virtual bool TryReplace(object target, string segment, JsonSerializerOptions serializerOptions, object value, out string errorMessage)
    {
        if (!TryGetListTypeArgument(target, out var typeArgument, out errorMessage))
        {
            return false;
        }

        var count = GenericListOrJsonArrayUtilities.GetCount(target);
        if (!TryGetPositionInfo(count, segment, OperationType.Replace, out var positionInfo, out errorMessage))
        {
            return false;
        }

        if (!TryConvertValue(value, typeArgument, segment, serializerOptions, out var convertedValue, out errorMessage))
        {
            return false;
        }

        var indexToAddTo = positionInfo.Type == PositionType.EndOfList ? count - 1 : positionInfo.Index;
        GenericListOrJsonArrayUtilities.SetValueAt(target, indexToAddTo, convertedValue);

        errorMessage = null;
        return true;
    }

    public virtual bool TryTest(object target, string segment, JsonSerializerOptions serializerOptions, object value, out string errorMessage)
    {

        if (!TryGetListTypeArgument(target, out var typeArgument, out errorMessage))
        {
            return false;
        }

        var count = GenericListOrJsonArrayUtilities.GetCount(target);

        if (!TryGetPositionInfo(count, segment, OperationType.Replace, out var positionInfo, out errorMessage))
        {
            return false;
        }

        if (!TryConvertValue(value, typeArgument, segment, serializerOptions, out var convertedValue, out errorMessage))
        {
            return false;
        }

        var currentValue = GenericListOrJsonArrayUtilities.GetElementAt(target, positionInfo.Index);

        if (!JsonUtilities.DeepEquals(currentValue, convertedValue, serializerOptions))
        {
            errorMessage = Resources.FormatValueAtListPositionNotEqualToTestValue(currentValue, value, positionInfo.Index);
            return false;
        }
        else
        {
            errorMessage = null;
            return true;
        }
    }

    public virtual bool TryTraverse(object target, string segment, JsonSerializerOptions serializerOptions, out object value, out string errorMessage)
    {
        var list = target as IList;
        if (list == null)
        {
            value = null;
            errorMessage = null;
            return false;
        }

        if (!int.TryParse(segment, out var index))
        {
            value = null;
            errorMessage = Resources.FormatInvalidIndexValue(segment);
            return false;
        }

        if (index < 0 || index >= list.Count)
        {
            value = null;
            errorMessage = Resources.FormatIndexOutOfBounds(segment);
            return false;
        }

        value = list[index];
        errorMessage = null;
        return true;
    }

    protected virtual bool TryConvertValue(object originalValue, Type listTypeArgument, string segment, out object convertedValue, out string errorMessage)
    {
        return TryConvertValue(originalValue, listTypeArgument, segment, null, out convertedValue, out errorMessage);
    }

    protected virtual bool TryConvertValue(object originalValue, Type listTypeArgument, string segment, JsonSerializerOptions serializerOptions, out object convertedValue, out string errorMessage)
    {
        var conversionResult = ConversionResultProvider.ConvertTo(originalValue, listTypeArgument, serializerOptions);
        if (!conversionResult.CanBeConverted)
        {
            convertedValue = null;
            errorMessage = Resources.FormatInvalidValueForProperty(originalValue);
            return false;
        }

        convertedValue = conversionResult.ConvertedInstance;
        errorMessage = null;
        return true;
    }

    private static bool TryGetListTypeArgument(object list, out Type listTypeArgument, out string errorMessage)
    {
        var listType = list.GetType();
        if (listType.IsArray)
        {
            errorMessage = $"The type '{listType.FullName}' which is an array is not supported for json patch operations as it has a fixed size.";
            listTypeArgument = null;
            return false;
        }

        var genericList = ClosedGenericMatcher.ExtractGenericInterface(listType, typeof(IList<>));
        if (genericList == null)
        {
            errorMessage = $"The type '{listType.FullName}' which is a non generic list is not supported for json patch operations. Only generic list types are supported.";
            listTypeArgument = null;
            return false;
        }

        listTypeArgument = genericList.GenericTypeArguments[0];
        errorMessage = null;
        return true;
    }

    protected virtual bool TryGetPositionInfo(int collectionCount, string segment, OperationType operationType, out PositionInfo positionInfo, out string errorMessage)
    {
        if (segment == "-")
        {
            positionInfo = new PositionInfo(PositionType.EndOfList, -1);
            errorMessage = null;
            return true;
        }

        if (int.TryParse(segment, out var position))
        {
            if (position >= 0 && position < collectionCount)
            {
                positionInfo = new PositionInfo(PositionType.Index, position);
                errorMessage = null;
                return true;
            }

            // As per JSON Patch spec, for Add operation the index value representing the number of elements is valid,
            // where as for other operations like Remove, Replace, Move and Copy the target index MUST exist.
            if (position == collectionCount && operationType == OperationType.Add)
            {
                positionInfo = new PositionInfo(PositionType.EndOfList, -1);
                errorMessage = null;
                return true;
            }

            positionInfo = new PositionInfo(PositionType.OutOfBounds, position);
            errorMessage = Resources.FormatIndexOutOfBounds(segment);
            return false;
        }
        else
        {
            positionInfo = new PositionInfo(PositionType.Invalid, -1);
            errorMessage = Resources.FormatInvalidIndexValue(segment);
            return false;
        }
    }

    protected readonly struct PositionInfo
    {
        public PositionInfo(PositionType type, int index)
        {
            Type = type;
            Index = index;
        }

        public PositionType Type { get; }
        public int Index { get; }
    }

    protected enum PositionType
    {
        Index, // valid index
        EndOfList, // '-'
        Invalid, // Ex: not an integer
        OutOfBounds
    }

    protected enum OperationType
    {
        Add,
        Remove,
        Get,
        Replace
    }
}
