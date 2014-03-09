using System;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    public static class TypeExtensions
    {
        public static bool IsCompatibleWith([NotNull] this Type type, object value)
        {
            return (value == null && AllowsNullValue(type)) ||
                (value != null && type.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()));
        }

        public static bool IsNullableValueType([NotNull] this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public static bool AllowsNullValue([NotNull] this Type type)
        {
            return (!type.GetTypeInfo().IsValueType || IsNullableValueType(type));
        }

        public static bool HasStringConverter([NotNull] this Type type)
        {
            // TODO: This depends on TypeConverter which does not exist in the CoreCLR.
            // return TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));
            TypeInfo typeInfo = type.GetTypeInfo();
            if (typeInfo.IsPrimitive || type == typeof(string))
            {
                return true;
            }
            if (IsNullableValueType(type) && HasStringConverter(type.GenericTypeArguments[0]))
            {
                // Nullable<T> where T is a primitive type or has a type converter
                return true;
            }
            return false;
        }

        public static Type[] GetTypeArgumentsIfMatch([NotNull] Type closedType, Type matchingOpenType)
        {
            TypeInfo closedTypeInfo = closedType.GetTypeInfo();
            if (!closedTypeInfo.IsGenericType)
            {
                return null;
            }

            Type openType = closedType.GetGenericTypeDefinition();
            return (matchingOpenType == openType) ? closedTypeInfo.GenericTypeArguments : null;
        }
    }
}
