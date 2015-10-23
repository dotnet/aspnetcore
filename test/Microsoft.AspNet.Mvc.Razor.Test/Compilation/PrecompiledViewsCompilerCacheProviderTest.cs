// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.Extensions.PlatformAbstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class PrecompiledViewsCompilerCacheProviderTest
    {
        [Fact]
        public void IsValidRazorFileInfoCollection_ReturnsFalse_IfTypeIsAbstract()
        {
            // Arrange
            var type = typeof(AbstractRazorFileInfoCollection);

            // Act
            var result = PrecompiledViewsCompilerCacheProvider.IsValidRazorFileInfoCollection(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidRazorFileInfoCollection_ReturnsFalse_IfTypeHasGenericParameters()
        {
            // Arrange
            var type = typeof(GenericRazorFileInfoCollection<>);

            // Act
            var result = PrecompiledViewsCompilerCacheProvider.IsValidRazorFileInfoCollection(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidRazorFileInfoCollection_ReturnsFalse_IfTypeDoesNotDeriveFromRazorFileInfoCollection()
        {
            // Arrange
            var type = typeof(NonSubTypeRazorFileInfoCollection);

            // Act
            var result = PrecompiledViewsCompilerCacheProvider.IsValidRazorFileInfoCollection(type);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(typeof(ParameterConstructorRazorFileInfoCollection))]
        [InlineData(typeof(ViewCollection))]
        public void IsValidRazorFileInfoCollection_ReturnsTrue_IfTypeDerivesFromRazorFileInfoCollection(Type type)
        {
            // Act
            var result = PrecompiledViewsCompilerCacheProvider.IsValidRazorFileInfoCollection(type);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetPrecompiledViews_ReturnsTypesSpecifiedByRazorFileInfoCollections()
        {
            // Arrange
            var fileInfoCollections = new[] { new ViewCollection() };

            // Act
            var precompiledViews = PrecompiledViewsCompilerCacheProvider.GetPrecompiledViews(
                fileInfoCollections,
                Mock.Of<IAssemblyLoadContext>());

            // Assert
            Assert.Equal(2, precompiledViews.Count);

            Type type;
            Assert.True(precompiledViews.TryGetValue("Views/Home/Index.cshtml", out type));
            Assert.Equal(typeof(TestView1), type);

            Assert.True(precompiledViews.TryGetValue("Views/Home/About.cshtml", out type));
            Assert.Equal(typeof(TestView2), type);
        }


        private abstract class AbstractRazorFileInfoCollection : RazorFileInfoCollection
        {
        }

        private class GenericRazorFileInfoCollection<TVal> : RazorFileInfoCollection
        {
        }

        private class ParameterConstructorRazorFileInfoCollection : RazorFileInfoCollection
        {
            public ParameterConstructorRazorFileInfoCollection(string value)
            {
            }
        }

        private class NonSubTypeRazorFileInfoCollection : Controller
        {
        }

        private class ViewCollection : RazorFileInfoCollection
        {
            public ViewCollection()
            {
                FileInfos = new[]
                {
                    new RazorFileInfo
                    {
                        FullTypeName = typeof(TestView1).FullName,
                        RelativePath = "Views/Home/Index.cshtml"
                    },
                    new RazorFileInfo
                    {
                        FullTypeName = typeof(TestView2).FullName,
                        RelativePath = "Views/Home/About.cshtml"
                    },
                };
            }

            public override Assembly LoadAssembly(IAssemblyLoadContext loadContext)
            {
                return GetType().GetTypeInfo().Assembly;
            }
        }


        private class TestView1
        {
        }

        private class TestView2
        {
        }
    }
}
