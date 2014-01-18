// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// MSTest utility for testing that a given type has the expected properties such as being public, sealed, etc.
    /// </summary>
    public class TypeAssert
    {
        /// <summary>
        /// Specifies a set of type properties to test for using the <see cref="CheckProperty"/> method.
        /// This enumeration has a <see cref="FlagsAttribute"/> attribute that allows a bitwise combination of its member values.
        /// </summary>
        [Flags]
        public enum TypeProperties
        {
            /// <summary>
            /// Indicates that the type must be abstract.
            /// </summary>
            IsAbstract = 0x1,

            /// <summary>
            /// Indicates that the type must be a class.
            /// </summary>
            IsClass = 0x2,

            /// <summary>
            /// Indicates that the type must be a COM object.
            /// </summary>
            IsComObject = 0x4,

            /// <summary>
            /// Indicates that the type must be disposable.
            /// </summary>
            IsDisposable = 0x8,

            /// <summary>
            /// Indicates that the type must be an enum.
            /// </summary>
            IsEnum = 0x10,

            /// <summary>
            /// Indicates that the type must be a generic type.
            /// </summary>
            IsGenericType = 0x20,

            /// <summary>
            /// Indicates that the type must be a generic type definition.
            /// </summary>
            IsGenericTypeDefinition = 0x40,

            /// <summary>
            /// Indicates that the type must be an interface.
            /// </summary>
            IsInterface = 0x80,

            /// <summary>
            /// Indicates that the type must be nested and declared private.
            /// </summary>
            IsNestedPrivate = 0x100,

            /// <summary>
            /// Indicates that the type must be nested and declared public.
            /// </summary>
            IsNestedPublic = 0x200,

            /// <summary>
            /// Indicates that the type must be public.
            /// </summary>
            IsPublic = 0x400,

            /// <summary>
            /// Indicates that the type must be sealed.
            /// </summary>
            IsSealed = 0x800,

            /// <summary>
            /// Indicates that the type must be visible outside the assembly.
            /// </summary>
            IsVisible = 0x1000,

            /// <summary>
            /// Indicates that the type must be static.
            /// </summary>
            IsStatic = TypeAssert.TypeProperties.IsAbstract | TypeAssert.TypeProperties.IsSealed,

            /// <summary>
            /// Indicates that the type must be a public, visible class.
            /// </summary>
            IsPublicVisibleClass = TypeAssert.TypeProperties.IsClass | TypeAssert.TypeProperties.IsPublic | TypeAssert.TypeProperties.IsVisible
        }

        private static void CheckProperty(Type type, bool expected, bool actual, string property)
        {
            Assert.NotNull(type);
            Assert.True(expected == actual, String.Format("Type '{0}' should{1} be {2}.", type.FullName, expected ? "" : " NOT", property));
        }

        /// <summary>
        /// Determines whether the specified type has a given set of properties such as being public, sealed, etc.
        /// The method asserts if one or more of the properties are not satisfied.
        /// </summary>
        /// <typeparam name="T">The type to test for properties.</typeparam>
        /// <param name="typeProperties">The set of type properties to test for.</param>
        public void HasProperties<T>(TypeProperties typeProperties)
        {
            HasProperties(typeof(T), typeProperties);
        }

        /// <summary>
        /// Determines whether the specified type has a given set of properties such as being public, sealed, etc.
        /// The method asserts if one or more of the properties are not satisfied.
        /// </summary>
        /// <typeparam name="T">The type to test for properties.</typeparam>
        /// <typeparam name="TIsAssignableFrom">Verify that the type to test is assignable from this type.</typeparam>
        /// <param name="typeProperties">The set of type properties to test for.</param>
        public void HasProperties<T, TIsAssignableFrom>(TypeProperties typeProperties)
        {
            HasProperties(typeof(T), typeProperties, typeof(TIsAssignableFrom));
        }

        /// <summary>
        /// Determines whether the specified type has a given set of properties such as being public, sealed, etc.
        /// The method asserts if one or more of the properties are not satisfied.
        /// </summary>
        /// <param name="type">The type to test for properties.</param>
        /// <param name="typeProperties">The set of type properties to test for.</param>
        public void HasProperties(Type type, TypeProperties typeProperties)
        {
            HasProperties(type, typeProperties, null);
        }

        /// <summary>
        /// Determines whether the specified type has a given set of properties such as being public, sealed, etc.
        /// The method asserts if one or more of the properties are not satisfied.
        /// </summary>
        /// <param name="type">The type to test for properties.</param>
        /// <param name="typeProperties">The set of type properties to test for.</param>
        /// <param name="isAssignableFrom">Verify that the type to test is assignable from this type.</param>
        public void HasProperties(Type type, TypeProperties typeProperties, Type isAssignableFrom)
        {
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsAbstract) > 0, type.IsAbstract, "abstract");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsClass) > 0, type.IsClass, "a class");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsComObject) > 0, type.IsCOMObject, "a COM object");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsDisposable) > 0, typeof(IDisposable).IsAssignableFrom(type), "disposable");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsEnum) > 0, type.IsEnum, "an enum");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsGenericType) > 0, type.IsGenericType, "a generic type");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsGenericTypeDefinition) > 0, type.IsGenericTypeDefinition, "a generic type definition");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsInterface) > 0, type.IsInterface, "an interface");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsNestedPrivate) > 0, type.IsNestedPrivate, "nested private");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsNestedPublic) > 0, type.IsNestedPublic, "nested public");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsPublic) > 0, type.IsPublic, "public");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsSealed) > 0, type.IsSealed, "sealed");
            TypeAssert.CheckProperty(type, (typeProperties & TypeProperties.IsVisible) > 0, type.IsVisible, "visible");
            if (isAssignableFrom != null)
            {
                TypeAssert.CheckProperty(type, true, isAssignableFrom.IsAssignableFrom(type), String.Format("assignable from {0}", isAssignableFrom.FullName));
            }
        }
    }
}