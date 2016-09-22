// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class ListAdapter : IAdapter
    {
        public bool TryAdd(
            object target,
            string segment,
            IContractResolver contractResolver,
            object value,
            out string errorMessage)
        {
            var list = (IList)target;

            Type typeArgument = null;
            if (!TryGetListTypeArgument(list, out typeArgument, out errorMessage))
            {
                return false;
            }

            PositionInfo positionInfo;
            if (!TryGetPositionInfo(list, segment, out positionInfo, out errorMessage))
            {
                return false;
            }

            object convertedValue = null;
            if (!TryConvertValue(value, typeArgument, segment, out convertedValue, out errorMessage))
            {
                return false;
            }

            if (positionInfo.Type == PositionType.EndOfList)
            {
                list.Add(convertedValue);
            }
            else
            {
                list.Insert(positionInfo.Index, convertedValue);
            }

            errorMessage = null;
            return true;
        }

        public bool TryGet(
            object target,
            string segment,
            IContractResolver contractResolver,
            out object value,
            out string errorMessage)
        {
            var list = (IList)target;

            Type typeArgument = null;
            if (!TryGetListTypeArgument(list, out typeArgument, out errorMessage))
            {
                value = null;
                return false;
            }

            PositionInfo positionInfo;
            if (!TryGetPositionInfo(list, segment, out positionInfo, out errorMessage))
            {
                value = null;
                return false;
            }

            if (positionInfo.Type == PositionType.EndOfList)
            {
                value = list[list.Count - 1];
            }
            else
            {
                value = list[positionInfo.Index];
            }

            errorMessage = null;
            return true;
        }

        public bool TryRemove(
            object target,
            string segment,
            IContractResolver contractResolver,
            out string errorMessage)
        {
            var list = (IList)target;

            Type typeArgument = null;
            if (!TryGetListTypeArgument(list, out typeArgument, out errorMessage))
            {
                return false;
            }

            PositionInfo positionInfo;
            if (!TryGetPositionInfo(list, segment, out positionInfo, out errorMessage))
            {
                return false;
            }

            if (positionInfo.Type == PositionType.EndOfList)
            {
                list.RemoveAt(list.Count - 1);
            }
            else
            {
                list.RemoveAt(positionInfo.Index);
            }

            errorMessage = null;
            return true;
        }

        public bool TryReplace(
            object target,
            string segment,
            IContractResolver contractResolver,
            object value,
            out string errorMessage)
        {
            var list = (IList)target;

            Type typeArgument = null;
            if (!TryGetListTypeArgument(list, out typeArgument, out errorMessage))
            {
                return false;
            }

            PositionInfo positionInfo;
            if (!TryGetPositionInfo(list, segment, out positionInfo, out errorMessage))
            {
                return false;
            }

            object convertedValue = null;
            if (!TryConvertValue(value, typeArgument, segment, out convertedValue, out errorMessage))
            {
                return false;
            }

            if (positionInfo.Type == PositionType.EndOfList)
            {
                list[list.Count - 1] = convertedValue;
            }
            else
            {
                list[positionInfo.Index] = convertedValue;
            }

            errorMessage = null;
            return true;
        }

        public bool TryTraverse(
            object target,
            string segment,
            IContractResolver contractResolver,
            out object value,
            out string errorMessage)
        {
            var list = target as IList;
            if (list == null)
            {
                value = null;
                errorMessage = null;
                return false;
            }

            int index = -1;
            if (!int.TryParse(segment, out index))
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

        private bool TryConvertValue(
            object originalValue,
            Type listTypeArgument,
            string segment,
            out object convertedValue,
            out string errorMessage)
        {
            var conversionResult = ConversionResultProvider.ConvertTo(originalValue, listTypeArgument);
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

        private bool TryGetListTypeArgument(IList list, out Type listTypeArgument, out string errorMessage)
        {
            // Arrays are not supported as they have fixed size and operations like Add, Insert do not make sense
            var listType = list.GetType();
            if (listType.IsArray)
            {
                errorMessage = Resources.FormatPatchNotSupportedForArrays(listType.FullName);
                listTypeArgument = null;
                return false;
            }
            else
            {
                var genericList = ClosedGenericMatcher.ExtractGenericInterface(listType, typeof(IList<>));
                if (genericList == null)
                {
                    errorMessage = Resources.FormatPatchNotSupportedForNonGenericLists(listType.FullName);
                    listTypeArgument = null;
                    return false;
                }
                else
                {
                    listTypeArgument = genericList.GenericTypeArguments[0];
                    errorMessage = null;
                    return true;
                }
            }
        }

        private bool TryGetPositionInfo(IList list, string segment, out PositionInfo positionInfo, out string errorMessage)
        {
            if (segment == "-")
            {
                positionInfo = new PositionInfo(PositionType.EndOfList, -1);
                errorMessage = null;
                return true;
            }

            int position = -1;
            if (int.TryParse(segment, out position))
            {
                if (position >= 0 && position < list.Count)
                {
                    positionInfo = new PositionInfo(PositionType.Index, position);
                    errorMessage = null;
                    return true;
                }
                else
                {
                    positionInfo = default(PositionInfo);
                    errorMessage = Resources.FormatIndexOutOfBounds(segment);
                    return false;
                }
            }
            else
            {
                positionInfo = default(PositionInfo);
                errorMessage = Resources.FormatInvalidIndexValue(segment);
                return false;
            }
        }

        private struct PositionInfo
        {
            public PositionInfo(PositionType type, int index)
            {
                Type = type;
                Index = index;
            }

            public PositionType Type { get; }
            public int Index { get; }
        }

        private enum PositionType
        {
            Index, // valid index
            EndOfList, // '-'
            Invalid, // Ex: not an integer
            OutOfBounds
        }
    }
}
