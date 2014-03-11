using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

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

        private static object ConvertSimpleType(CultureInfo culture, object value, Type destinationType)
        {
            if (value == null || value.GetType().IsAssignableFrom(destinationType))
            {
                return value;
            }

            // if this is a user-input value but the user didn't type anything, return no value
            var valueAsString = value as string;

            if (valueAsString != null && string.IsNullOrWhiteSpace(valueAsString))
            {
                return null;
            }

            if (destinationType == typeof(int))
            {
                return Convert.ToInt32(value);
            }
            else if (destinationType == typeof(bool))
            {
                return Boolean.Parse(value.ToString());
            }
            else if (destinationType == typeof(string))
            {
                return Convert.ToString(value);
            }
            string message = Resources.FormatValueProviderResult_NoConverterExists(value.GetType(), destinationType);
            throw new InvalidOperationException(message);

            // TODO: Revive once we get TypeConverters
            //TypeConverter converter = TypeDescriptor.GetConverter(destinationType);
            //bool canConvertFrom = converter.CanConvertFrom(value.GetType());
            //if (!canConvertFrom)
            //{
            //    converter = TypeDescriptor.GetConverter(value.GetType());
            //}
            //if (!(canConvertFrom || converter.CanConvertTo(destinationType)))
            //{
            //    // EnumConverter cannot convert integer, so we verify manually
            //    if (destinationType.GetTypeInfo().IsEnum && value is int)
            //    {
            //        return Enum.ToObject(destinationType, (int)value);
            //    }

            //    // In case of a Nullable object, we try again with its underlying type.
            //    Type underlyingType = Nullable.GetUnderlyingType(destinationType);
            //    if (underlyingType != null)
            //    {
            //        return ConvertSimpleType(culture, value, underlyingType);
            //    }

            //    throw Error.InvalidOperation(Resources.ValueProviderResult_NoConverterExists, value.GetType(), destinationType);
            //}

            //try
            //{
            //    return canConvertFrom
            //               ? converter.ConvertFrom(null, culture, value)
            //               : converter.ConvertTo(null, culture, value, destinationType);
            //}
            //catch (Exception ex)
            //{
            //    throw Error.InvalidOperation(ex, Resources.ValueProviderResult_ConversionThrew, value.GetType(), destinationType);
            //}
        }

        private static object UnwrapPossibleArrayType(CultureInfo culture, object value, Type destinationType)
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
    }
}
