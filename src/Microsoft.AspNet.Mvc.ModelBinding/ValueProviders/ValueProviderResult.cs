// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ValueProviderResult
    {
        private static readonly CultureInfo _staticCulture = CultureInfo.InvariantCulture;
        private CultureInfo _instanceCulture;

        // default constructor so that subclassed types can set the properties themselves
        protected ValueProviderResult()
        {
        }

        public ValueProviderResult(object rawValue, string attemptedValue, CultureInfo culture)
        {
            RawValue = rawValue;
            AttemptedValue = attemptedValue;
            Culture = culture;
        }

        public string AttemptedValue { get; protected set; }

        public CultureInfo Culture
        {
            get
            {
                if (_instanceCulture == null)
                {
                    _instanceCulture = _staticCulture;
                }
                return _instanceCulture;
            }
            protected set { _instanceCulture = value; }
        }

        public object RawValue { get; protected set; }

        public object ConvertTo(Type type)
        {
            return ConvertTo(type, culture: null);
        }

        public virtual object ConvertTo([NotNull] Type type, CultureInfo culture)
        {
            var value = RawValue;
            if (value == null)
            {
                // treat null route parameters as though they were the default value for the type
                return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) :
                                                        null;
            }

            if (value.GetType().IsAssignableFrom(type))
            {
                return value;
            }

            var cultureToUse = culture ?? Culture;
            return UnwrapPossibleArrayType(cultureToUse, value, type);
        }

        public static bool CanConvertFromString(Type destinationType)
        {
            return TypeHelper.IsSimpleType(UnwrapNullableType(destinationType)) ||
                   TypeHelper.HasStringConverter(destinationType);
        }

        private object UnwrapPossibleArrayType(CultureInfo culture, object value, Type destinationType)
        {
            // array conversion results in four cases, as below
            var valueAsArray = value as Array;
            if (destinationType.IsArray)
            {
                var destinationElementType = destinationType.GetElementType();
                if (valueAsArray != null)
                {
                    // case 1: both destination + source type are arrays, so convert each element
                    var converted = (IList)Array.CreateInstance(destinationElementType, valueAsArray.Length);
                    for (var i = 0; i < valueAsArray.Length; i++)
                    {
                        converted[i] = ConvertSimpleType(culture, valueAsArray.GetValue(i), destinationElementType);
                    }
                    return converted;
                }
                else
                {
                    // case 2: destination type is array but source is single element, so wrap element in
                    // array + convert
                    var element = ConvertSimpleType(culture, value, destinationElementType);
                    var converted = (IList)Array.CreateInstance(destinationElementType, 1);
                    converted[0] = element;
                    return converted;
                }
            }
            else if (valueAsArray != null)
            {
                // case 3: destination type is single element but source is array, so extract first element + convert
                if (valueAsArray.Length > 0)
                {
                    value = valueAsArray.GetValue(0);
                    return ConvertSimpleType(culture, value, destinationType);
                }
                else
                {
                    // case 3(a): source is empty array, so can't perform conversion
                    return null;
                }
            }

            // case 4: both destination + source type are single elements, so convert
            return ConvertSimpleType(culture, value, destinationType);
        }

        private object ConvertSimpleType(CultureInfo culture, object value, Type destinationType)
        {
            if (value == null || value.GetType().IsAssignableFrom(destinationType))
            {
                return value;
            }

            // In case of a Nullable object, we try again with its underlying type.
            destinationType = UnwrapNullableType(destinationType);

            // if this is a user-input value but the user didn't type anything, return no value
            var valueAsString = value as string;
            if (valueAsString != null && string.IsNullOrWhiteSpace(valueAsString))
            {
                return null;
            }

            var converter = TypeDescriptor.GetConverter(destinationType);
            var canConvertFrom = converter.CanConvertFrom(value.GetType());
            if (!canConvertFrom)
            {
                converter = TypeDescriptor.GetConverter(value.GetType());
            }
            if (!(canConvertFrom || converter.CanConvertTo(destinationType)))
            {
                // EnumConverter cannot convert integer, so we verify manually
                if (destinationType.IsEnum() && (value is int))
                {
                    return Enum.ToObject(destinationType, (int)value);
                }

                throw new InvalidOperationException(
                    Resources.FormatValueProviderResult_NoConverterExists(value.GetType(), destinationType));
            }

            try
            {
                return canConvertFrom
                           ? converter.ConvertFrom(null, culture, value)
                           : converter.ConvertTo(null, culture, value, destinationType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    Resources.FormatValueProviderResult_ConversionThrew(value.GetType(), destinationType), ex);
            }
        }

        private static Type UnwrapNullableType(Type destinationType)
        {
            return Nullable.GetUnderlyingType(destinationType) ?? destinationType;
        }
    }
}
