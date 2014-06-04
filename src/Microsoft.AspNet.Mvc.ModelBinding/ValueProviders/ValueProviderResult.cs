// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
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
            return GetConverterDelegate(destinationType) != null;
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

            var converter = GetConverterDelegate(destinationType);
            if (converter == null)
            {
                var message = Resources.FormatValueProviderResult_NoConverterExists(value.GetType(), destinationType);
                throw new InvalidOperationException(message);
            }

            return converter(value, culture);
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
                    IList converted = Array.CreateInstance(destinationElementType, valueAsArray.Length);
                    for (var i = 0; i < valueAsArray.Length; i++)
                    {
                        converted[i] = ConvertSimpleType(culture, valueAsArray.GetValue(i), destinationElementType);
                    }
                    return converted;
                }
                else
                {
                    // case 2: destination type is array but source is single element, so wrap element in array + convert
                    var element = ConvertSimpleType(culture, value, destinationElementType);
                    IList converted = Array.CreateInstance(destinationElementType, 1);
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

        private static Func<object, CultureInfo, object> GetConverterDelegate(Type destinationType)
        {
            destinationType = UnwrapNullableType(destinationType);

            if (destinationType == typeof(string))
            {
                return (value, culture) => Convert.ToString(value, culture);
            }

            if (destinationType == typeof(int))
            {
                return (value, culture) => Convert.ToInt32(value, culture);
            }

            if (destinationType == typeof(long))
            {
                return (value, culture) => Convert.ToInt64(value, culture);
            }

            if (destinationType == typeof(float))
            {
                return (value, culture) => Convert.ToSingle(value, culture);
            }

            if (destinationType == typeof(double))
            {
                return (value, culture) => Convert.ToDouble(value, culture);
            }

            if (destinationType == typeof(decimal))
            {
                return (value, culture) => Convert.ToDecimal(value, culture);
            }

            if (destinationType == typeof(bool))
            {
                return (value, culture) => Convert.ToBoolean(value, culture);
            }

            if (destinationType == typeof(DateTime))
            {
                return (value, culture) =>
                {
                    ThrowIfNotStringType(value, destinationType);
                    return DateTime.Parse((string)value, culture);
                };
            }

            if (destinationType == typeof(DateTimeOffset))
            {
                return (value, culture) =>
                {
                    ThrowIfNotStringType(value, destinationType);
                    return DateTimeOffset.Parse((string)value, culture);
                };
            }

            if (destinationType == typeof(TimeSpan))
            {
                return (value, culture) =>
                {
                    ThrowIfNotStringType(value, destinationType);
                    return TimeSpan.Parse((string)value, culture);
                };
            }

            if (destinationType == typeof(Guid))
            {
                return (value, culture) =>
                {
                    ThrowIfNotStringType(value, destinationType);
                    return Guid.Parse((string)value);
                };
            }

            if (destinationType.GetTypeInfo().IsEnum)
            {
                return (value, culture) =>
                {
                    // EnumConverter cannot convert integer, so we verify manually
                    if ((value is int))
                    {
                        if (Enum.IsDefined(destinationType, value))
                        {
                            return Enum.ToObject(destinationType, (int)value);
                        }

                        throw new FormatException(
                            Resources.FormatValueProviderResult_CannotConvertEnum(value,
                                                                                  destinationType));
                    }
                    else
                    {
                        ThrowIfNotStringType(value, destinationType);
                        return Enum.Parse(destinationType, (string)value);
                    }
                };
            }

            return null;
        }

        private static Type UnwrapNullableType(Type destinationType)
        {
            return Nullable.GetUnderlyingType(destinationType) ?? destinationType;
        }

        private static void ThrowIfNotStringType(object value, Type destinationType)
        {
            var type = value.GetType();
            if (type != typeof(string))
            {
                var message = Resources.FormatValueProviderResult_NoConverterExists(type, destinationType);
                throw new InvalidOperationException(message);
            }
        }
    }
}
