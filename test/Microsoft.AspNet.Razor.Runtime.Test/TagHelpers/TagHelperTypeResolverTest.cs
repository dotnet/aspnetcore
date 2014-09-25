// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperTypeResolverTest
    {
        protected static readonly Type[] ValidTestableTagHelpers = new[]
        {
            typeof(Valid_PlainTagHelper),
            typeof(Valid_InheritedTagHelper)
        };

        protected static readonly Type[] InvalidTestableTagHelpers = new[]
        {
            typeof(Invalid_AbstractTagHelper),
            typeof(Invalid_GenericTagHelper<>),
            typeof(Invalid_NestedPublicTagHelper),
            typeof(Invalid_NestedInternalTagHelper),
            typeof(Invalid_PrivateTagHelper),
            typeof(Invalid_ProtectedTagHelper),
            typeof(Invalid_InternalTagHelper)
        };

        protected static readonly Type[] TestableTagHelpers =
            ValidTestableTagHelpers.Concat(InvalidTestableTagHelpers).ToArray();

        [Fact]
        public void TypeResolver_ThrowsWhenCannotResolveAssembly()
        {
            // Arrange
            var tagHelperTypeResolver = new TagHelperTypeResolver();
            var expectedErrorMessage = string.Format(
                CultureInfo.InvariantCulture,
                "Cannot resolve TagHelper containing assembly '{0}'. Error: '{1}'.",
                "abcd",
                "Could not load file or assembly 'abcd' or one of its dependencies. " +
                "The system cannot find the file specified.");

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                tagHelperTypeResolver.Resolve("abcd");
            });

            Assert.Equal(expectedErrorMessage, ex.Message, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void TypeResolver_OnlyReturnsValidTagHelpersForAssemblyLookup()
        {
            // Arrange
            var tagHelperTypeResolver = new TestTagHelperTypeResolver(TestableTagHelpers);

            // Act
            var types = tagHelperTypeResolver.Resolve("Foo");

            // Assert
            Assert.Equal(ValidTestableTagHelpers, types);
        }

        [Fact]
        public void TypeResolver_ReturnsEmptyEnumerableIfNoValidTagHelpersFound()
        {
            // Arrange
            var tagHelperTypeResolver = new TestTagHelperTypeResolver(InvalidTestableTagHelpers);

            // Act
            var types = tagHelperTypeResolver.Resolve("Foo");

            // Assert
            Assert.Empty(types);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TypeResolver_ResolveThrowsIfEmptyOrNullLookupText(string name)
        {
            // Arrange
            var tagHelperTypeResolver = new TestTagHelperTypeResolver(InvalidTestableTagHelpers);
            var expectedMessage = "Tag helper directive assembly name cannot be null or empty." +
                Environment.NewLine +
                "Parameter name: name";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(nameof(name),
            () =>
            {
                tagHelperTypeResolver.Resolve(name);
            });

            Assert.Equal(expectedMessage, ex.Message);
        }

        protected class TestTagHelperTypeResolver : TagHelperTypeResolver
        {
            private IEnumerable<TypeInfo> _assemblyTypeInfos;

            public TestTagHelperTypeResolver(IEnumerable<Type> assemblyTypes)
            {
                _assemblyTypeInfos = assemblyTypes.Select(type => type.GetTypeInfo());
                OnGetLibraryDefinedTypes = (_) => { };
            }

            public Action<AssemblyName> OnGetLibraryDefinedTypes { get; set; }

            internal override IEnumerable<TypeInfo> GetLibraryDefinedTypes(AssemblyName assemblyName)
            {
                OnGetLibraryDefinedTypes(assemblyName);

                return _assemblyTypeInfos;
            }
        }

        public class Invalid_NestedPublicTagHelper : ITagHelper
        {
            public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
            {
                return Task.FromResult(result: true);
            }
        }

        internal class Invalid_NestedInternalTagHelper : ITagHelper
        {
            public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
            {
                return Task.FromResult(result: true);
            }
        }

        private class Invalid_PrivateTagHelper : ITagHelper
        {
            public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
            {
                return Task.FromResult(result: true);
            }
        }

        protected class Invalid_ProtectedTagHelper : ITagHelper
        {
            public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
            {
                return Task.FromResult(result: true);
            }
        }
    }

    // These tag helper types must be unnested and public to potentially be valid tag helpers.
    // In this case they do not fulfill other TagHelper requirements.
    public abstract class Invalid_AbstractTagHelper : ITagHelper
    {
        public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            return Task.FromResult(result: true);
        }
    }

    public class Invalid_GenericTagHelper<T> : ITagHelper
    {
        public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            return Task.FromResult(result: true);
        }
    }

    internal class Invalid_InternalTagHelper : ITagHelper
    {
        public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            return Task.FromResult(result: true);
        }
    }
}