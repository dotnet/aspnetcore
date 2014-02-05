using System;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    public static class TypeExtensions
    {
        public static bool IsCompatibleObject<T>(this object value)
        {
            return (value is T || (value == null && TypeAllowsNullValue(typeof(T))));
        }

        public static bool IsNullableValueType(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public static bool TypeAllowsNullValue(this Type type)
        {
            return (!type.GetTypeInfo().IsValueType || IsNullableValueType(type));
        }
    }
}
