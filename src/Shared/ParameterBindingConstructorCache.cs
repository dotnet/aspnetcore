// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;

namespace Microsoft.AspNetCore.Http;

internal sealed class ParameterBindingConstructorCache
{
    private readonly ConcurrentDictionary<Type, ConstructorInfo?> _constructorCache = new();

    public ConstructorInfo? GetParameterConstructor(ParameterInfo parameter)
    {
        static ConstructorInfo? GetConstructor(Type parameterType)
        {
            // Try to find the parameterless constructor
            var constructor = parameterType.GetConstructor(Array.Empty<Type>());
            if (constructor is not null)
            {
                return constructor;
            }

            // If a parameterless ctor is not defined
            // we will try to find a ctor that includes all
            // property types and in the right order.
            // Eg.:
            // public class Sample
            // {
            //    // Valid
            //    public Sample(int Id, string Name){}
            //    // Valid ???
            //    public Sample(int id, string name){}
            //    // InValid
            //    public Sample(string name, int id){}
            //
            //    public int Id { get; set; }
            //    public string Name { get; set; }
            //}

            var properties = parameterType.GetProperties();
            var types = new Type[properties.Length];
            for (var i = 0; i < properties.Length; i++)
            {
                types[i] = properties[i].PropertyType;
            }
            constructor = parameterType.GetConstructor(types);

            if (constructor is not null)
            {
                return constructor;
            }

            if (parameterType.IsValueType)
            {
                // Value types always have a default constructor, we will use
                // the parameter type during the NewExpression creation
                return null;
            }

            throw new InvalidOperationException($"No '{parameterType}' public parameterless constructor found.");
        }

        return _constructorCache.GetOrAdd(parameter.ParameterType, GetConstructor);
    }
}
