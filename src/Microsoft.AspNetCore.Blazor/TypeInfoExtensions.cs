using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Blazor
{
    public static class TypeInfoExtensions
    {
        public static IEnumerable<PropertyInfo> GetPropertiesIncludingInherited(this TypeInfo typeInfo, BindingFlags bindingFlags)
        {
            while (typeInfo != null)
            {
                var properties = typeInfo.GetProperties(bindingFlags)
                    .Where(prop => prop.ReflectedType == prop.DeclaringType);
                foreach (var property in properties)
                    yield return property;

                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }
    }
}