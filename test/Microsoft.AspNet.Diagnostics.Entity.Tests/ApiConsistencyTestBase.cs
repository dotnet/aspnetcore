// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Data.Entity
{
    public abstract class ApiConsistencyTestBase
    {
        protected const BindingFlags PublicInstance
            = BindingFlags.Instance | BindingFlags.Public;

        protected const BindingFlags AnyInstance
            = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Fact]
        public void Public_inheritable_apis_should_be_virtual()
        {
            var nonVirtualMethods
                = (from type in GetAllTypes(TargetAssembly.GetTypes())
                    where type.IsVisible
                          && !type.IsSealed
                          && type.GetConstructors(AnyInstance).Any(c => c.IsPublic || c.IsFamily || c.IsFamilyOrAssembly)
                          && type.Namespace != null
                          && !type.Namespace.EndsWith(".Compiled")
                    from method in type.GetMethods(PublicInstance)
                    where method.DeclaringType == type
                          && !(method.IsVirtual && !method.IsFinal)
                    select type.FullName + "." + method.Name)
                    .ToList();

            Assert.False(
                nonVirtualMethods.Any(),
                "\r\n-- Missing virtual APIs --\r\n" + string.Join("\r\n", nonVirtualMethods));
        }

        [Fact]
        public void Public_api_arguments_should_have_not_null_annotation()
        {
            var parametersMissingAttribute
                = (from type in GetAllTypes(TargetAssembly.GetTypes())
                    where type.IsVisible && !typeof(Delegate).IsAssignableFrom(type)
                    let interfaceMappings = type.GetInterfaces().Select(type.GetInterfaceMap)
                    let events = type.GetEvents()
                    from method in type.GetMethods(PublicInstance | BindingFlags.Static)
                        .Concat<MethodBase>(type.GetConstructors())
                    where method.DeclaringType == type
                    where type.IsInterface || !interfaceMappings.Any(im => im.TargetMethods.Contains(method))
                    where !events.Any(e => e.AddMethod == method || e.RemoveMethod == method)
                    from parameter in method.GetParameters()
                    where !parameter.ParameterType.IsValueType
                          && !parameter.GetCustomAttributes()
                              .Any(
                                  a => a.GetType().Name == "NotNullAttribute"
                                       || a.GetType().Name == "CanBeNullAttribute")
                    select type.FullName + "." + method.Name + "[" + parameter.Name + "]")
                    .ToList();

            Assert.False(
                parametersMissingAttribute.Any(),
                "\r\n-- Missing NotNull annotations --\r\n" + string.Join("\r\n", parametersMissingAttribute));
        }

        [Fact]
        public void Async_methods_should_have_overload_with_cancellation_token_and_end_with_async_suffix()
        {
            var asyncMethods
                = (from type in GetAllTypes(TargetAssembly.GetTypes())
                    where type.IsVisible
                    from method in type.GetMethods(PublicInstance | BindingFlags.Static)
                    where method.DeclaringType == type
                    where typeof(Task).IsAssignableFrom(method.ReturnType)
                    select method).ToList();

            var asyncMethodsWithToken
                = (from method in asyncMethods
                    where method.GetParameters().Any(pi => pi.ParameterType == typeof(CancellationToken))
                    select method).ToList();

            var asyncMethodsWithoutToken
                = (from method in asyncMethods
                    where method.GetParameters().All(pi => pi.ParameterType != typeof(CancellationToken))
                    select method).ToList();

            var missingOverloads
                = (from methodWithoutToken in asyncMethodsWithoutToken
                    where !asyncMethodsWithToken
                        .Any(methodWithToken => methodWithoutToken.Name == methodWithToken.Name
                                                && methodWithoutToken.ReflectedType == methodWithToken.ReflectedType)
                    // ReSharper disable once PossibleNullReferenceException
                    select methodWithoutToken.DeclaringType.Name + "." + methodWithoutToken.Name)
                    .Except(GetCancellationTokenExceptions())
                    .ToList();

            Assert.False(
                missingOverloads.Any(),
                "\r\n-- Missing async overloads --\r\n" + string.Join("\r\n", missingOverloads));

            var missingSuffixMethods
                = asyncMethods
                    .Where(method => !method.Name.EndsWith("Async"))
                    .Select(method => method.DeclaringType.Name + "." + method.Name)
                    .Except(GetAsyncSuffixExceptions())
                    .ToList();

            Assert.False(
                missingSuffixMethods.Any(),
                "\r\n-- Missing async suffix --\r\n" + string.Join("\r\n", missingSuffixMethods));
        }

        protected virtual IEnumerable<string> GetCancellationTokenExceptions()
        {
            return Enumerable.Empty<string>();
        }

        protected virtual IEnumerable<string> GetAsyncSuffixExceptions()
        {
            return Enumerable.Empty<string>();
        }

        protected abstract Assembly TargetAssembly { get; }

        protected virtual IEnumerable<Type> GetAllTypes(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                yield return type;

                foreach (var nestedType in GetAllTypes(type.GetNestedTypes()))
                {
                    yield return nestedType;
                }
            }
        }
    }
}
